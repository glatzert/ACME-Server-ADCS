using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

/// <summary>
/// Validates dns-persist-01 challenges as described in the draft (https://datatracker.ietf.org/doc/draft-ietf-acme-dns-persist/01/)
/// The content generally looks like: &lt;acme-caa-identity&gt;;accountUri=&lt;account-uri&gt;;persistUntil=&lt;unix-epoch-seconds&gt;;policy=&lt;policy1&gt;...
/// </summary>
public class DnsPersist01ChallengeValidator(
    [FromKeyedServices(nameof(Dns01ChallengeValidator))] ILookupClient lookupClient,
    TimeProvider timeProvider,
    ILogger<DnsPersist01ChallengeValidator> logger)
    : ChallengeValidator(logger)
{
    private readonly ILookupClient _lookupClient = lookupClient;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<DnsPersist01ChallengeValidator> _logger = logger;

    public const string AuthorizationDomainNameLabel = "_validation-persist";
    public const string WildcardPolicy = "wildcard";

    public override string ChallengeType => ChallengeTypes.DnsPersist01;

    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.DNS];


    protected override async Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        if (challenge is not DnsPersistChallenge dnsPersistChallenge)
        {
            _logger.LogError("Challenge is not of type DnsPersistChallenge.");
            throw new InvalidOperationException("Challenge is not of type DnsPersistChallenge.");
        }

        var dnsIdentifierValue = challenge.Authorization.Identifier.Value;

        var dnsPersistRecords = await QueryAuthorizationDomains(dnsIdentifierValue, cancellationToken);
        var utcNow = _timeProvider.GetUtcNow();

        if (!dnsPersistRecords.Any())
        {
            _logger.DnsPersist01ChallengeNoCandidatesFound(challenge.Authorization.Identifier.Value);
            return ChallengeValidationResult.Invalid(
                AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, $"Could not locate any {AuthorizationDomainNameLabel} TXT records.")
            );
        }

        _logger.DnsPersist01ChallengeCandidates(string.Join("; ", dnsPersistRecords));
        foreach (var record in dnsPersistRecords)
        {
            // The record was expired
            if (record.ValidUntil.HasValue && record.ValidUntil.Value < utcNow)
            {
                _logger.DnsPersist01ChallengeCandidateRemoved(record.ToString(), "persistUntil expiry");
                continue;
            }

            // The record was not for this server
            if (!dnsPersistChallenge.IssuerDomainNames.Contains(record.Issuer, StringComparer.OrdinalIgnoreCase))
            {
                _logger.DnsPersist01ChallengeCandidateRemoved(record.ToString(), "issuer mismatch");
                continue;
            }

            // The account uri did not match
            if (!record.AccountUri.Equals(dnsPersistChallenge.AccountUri))
            {
                _logger.DnsPersist01ChallengeCandidateRemoved(record.ToString(), "accountUri mismatch");
                continue;
            }

            // If the identifier is a wildcard, the TXT record origin has to match the identifier value and have a wildcard policy
            if (challenge.Authorization.IsWildcard)
            {
                if (!record.DomainName.Equals($"{AuthorizationDomainNameLabel}.{dnsIdentifierValue}"))
                {
                    _logger.DnsPersist01ChallengeCandidateRemoved(record.ToString(), "not suitable for wildcard identifier");
                    continue;
                }

                if(!record.HasPolicy(WildcardPolicy))
                {
                    _logger.DnsPersist01ChallengeCandidateRemoved(record.ToString(), "wildcard policy missing");
                    continue;
                }
            }

            // If the identifier does not match the TXT records origin, we need the parent Domain name and a wildcard policy
            // Since the record is a dns response, it will always end with a dot
            if (!record.DomainName.Equals($"{AuthorizationDomainNameLabel}.{dnsIdentifierValue}"))
            {
                var dnsIdentifierParent = dnsIdentifierValue.Split(".", 2).Last();
                if (!record.DomainName.Equals($"{AuthorizationDomainNameLabel}.{dnsIdentifierParent}"))
                {
                    _logger.DnsPersist01ChallengeCandidateRemoved(record.ToString(), "parent domain needed.");
                    continue;
                }

                if (!record.HasPolicy(WildcardPolicy))
                {
                    _logger.DnsPersist01ChallengeCandidateRemoved(record.ToString(), "parent wildcard policy missing");
                    continue;
                }
            }

            // If we got here, we hava a match!
            return ChallengeValidationResult.Valid();
        }

        return ChallengeValidationResult.Invalid(
            AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, $"Could not locate a valid {AuthorizationDomainNameLabel} TXT record.")
        );
    }

    private async Task<List<DnsPersist01Record>> QueryAuthorizationDomains(string domainName, CancellationToken cancellationToken)
    {
        List<string> authorizationDomainNames = [$"{AuthorizationDomainNameLabel}.{domainName}"];
        if(domainName.Count(x => x == '.') > 1)
        {
            var parentDomainName = domainName.Split(".", 2).Last();
            authorizationDomainNames.Add($"{AuthorizationDomainNameLabel}.{parentDomainName}");
        }

        var queryResults = new List<(string DomainName, string TxtContent)>();
        foreach(var authorizationDomain in authorizationDomainNames)
        {
            _logger.AttemptingToLoadDnsPersist01ChallengeResponse(authorizationDomain);
            var dnsQueryResult = await _lookupClient.QueryAsync(authorizationDomain, QueryType.TXT, cancellationToken: cancellationToken);

            var domainQueryResults = dnsQueryResult.Answers
                .OfType<DnsClient.Protocol.TxtRecord>()
                .SelectMany(r => r.Text, (record, txt) => (record.DomainName.Value, txt));

            foreach(var (domain, txtContent) in domainQueryResults)
            {
                _logger.DnsPersist01ChallengeResponseLoaded(domain, txtContent);
            }

            if (!domainQueryResults.Any())
            {
                _logger.NoDnsPersist01ChallengeResponseFound(authorizationDomain);
            }

            queryResults.AddRange(domainQueryResults);    
        }

        

        var dnsPersistRecords = new List<DnsPersist01Record>();
        foreach (var record in queryResults)
        {
            if (TryParseRecord(record.DomainName, record.TxtContent, out var dnsPersistRecord))
            {
                dnsPersistRecords.Add(dnsPersistRecord);
            }
        }

        return dnsPersistRecords;
    }

    internal bool TryParseRecord(string domainName, string record, [NotNullWhen(true)] out DnsPersist01Record? result)
    {
        const string AccountParameter = "accountUri";
        const string PolicyParameter = "policy";
        const string PersistUntilParameter = "persistUntil";

        result = null;

        var parts = record.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // We need the first part to be the issuer and the second part to be the account URI at minimum
        if (parts is not [var issuer, .. var parameterStrings])
        {
            _logger.DnsPersist01ChallengeResponseParseFailedForm(record);
            return false;
        }

        var parameters = parameterStrings
            .Select(x => x.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length == 2)
            .ToLookup(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);

        if (parameters[AccountParameter].Count() != 1)
        {
            _logger.DnsPersist01ChallengeResponseParseFailedAccountUri(record);
            return false;
        }

        if (parameters[PersistUntilParameter].Count() > 1)
        {
            _logger.DnsPersist01ChallengeResponseParseFailedPersistUntil(record);
            return false;
        }

        var accountUri = parameters[AccountParameter].First();
        var persistUntilUnixEpochString = parameters[PersistUntilParameter].FirstOrDefault();
        var policies = parameters[PolicyParameter].ToArray();

        DateTimeOffset? persistUntil = null;
        if (persistUntilUnixEpochString is not null)
        {
            if (!long.TryParse(persistUntilUnixEpochString, CultureInfo.InvariantCulture, out var persistUntilUnixEpoch))
            {
                _logger.DnsPersist01ChallengeResponseParseFailedPersistUntilNotLong(record, persistUntilUnixEpochString);
                return false;
            }

            persistUntil = DateTimeOffset.FromUnixTimeSeconds(persistUntilUnixEpoch);
        }


        result = new()
        {
            DomainName = domainName.TrimEnd('.'),
            Issuer = issuer,
            AccountUri = accountUri,
            Policies = policies,
            ValidUntil = persistUntil
        };

        _logger.DnsPersist01ChallengeResponseParsed(
            record, 
            result.Issuer, 
            result.AccountUri, 
            result.ValidUntil?.ToString("O"), 
            string.Join(", ", result.Policies));

        return true;
    }
}

internal class DnsPersist01Record
{
    public required string DomainName { get; set; }
    public required string Issuer { get; set; }
    public required string AccountUri { get; set; }
    public string[] Policies { get; set; } = [];

    public DateTimeOffset? ValidUntil { get; set; }


    public bool HasPolicy(string policy)
        => Policies.Contains(policy);

    public override string ToString()
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(DomainName)
            .Append("[TXT]: ")
            .Append(Issuer)
            .Append("; accountUri=")
            .Append(AccountUri);

        if (ValidUntil.HasValue)
        {
            builder.Append("; persistUntil=")
                .Append(ValidUntil.Value.ToUnixTimeSeconds());
        }

        if (Policies.Any())
        {
            builder.Append("; policy=")
                .Append(string.Join(",", Policies));
        }

        return builder.ToString();
    }
}