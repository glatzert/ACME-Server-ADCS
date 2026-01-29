using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

public class DnsPersist01ChallengeValidator(
    [FromKeyedServices(nameof(Dns01ChallengeValidator))] ILookupClient lookupClient,
    TimeProvider timeProvider,
    ILogger<DnsPersist01ChallengeValidator> logger)
    : ChallengeValidator(logger)
{
    private readonly ILookupClient _lookupClient = lookupClient;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<DnsPersist01ChallengeValidator> _logger = logger;

    private const string AuthorizationDomainNameLabel = "_validation-persist";
    private const string WildcardPolicy = "wildcard";

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

        foreach (var record in dnsPersistRecords)
        {
            // The record was not for this server
            if (!dnsPersistChallenge.IssuerDomainNames.Contains(record.Issuer))
            {
                continue;
            }

            // AccountUris will look like: "https://acme.th11s.de/account/<accountId>" and since accountId is a UUID, we can just check the ending of the URI
            // This is especially true, since we check the issuer domain before
            if (!record.AccountUri.EndsWith($"/{account.AccountId.Value}"))
            {
                continue;
            }

            // If the identifier is a wildcard, the TXT record origin has to match the identifier value and have a wildcard policy
            if (challenge.Authorization.IsWildcard)
            {
                if (!record.DomainName.Equals($"{AuthorizationDomainNameLabel}.{dnsIdentifierValue}"))
                {
                    continue;
                }

                if(!record.HasPolicy(WildcardPolicy))
                {
                    continue;
                }
            }

            // If the identifier does not match the TXT records origin, we need the parent Domain name and a wildcard policy
            if (!record.DomainName.Equals($"{AuthorizationDomainNameLabel}.{dnsIdentifierValue}"))
            {
                var dnsIdentifierParent = dnsIdentifierValue.Split(".", 2).Last();
                if (!record.DomainName.Equals($"{AuthorizationDomainNameLabel}.{dnsIdentifierParent}"))
                {
                    continue;
                }

                if (!record.HasPolicy(WildcardPolicy))
                {
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
            _logger.LogDebug("Querying DNS for TXT records at '{AuthDomainName}'", authorizationDomain);
            var dnsQueryResult = await _lookupClient.QueryAsync(authorizationDomain, QueryType.TXT, cancellationToken: cancellationToken);

            queryResults.AddRange(
                dnsQueryResult.Answers
                    .OfType<DnsClient.Protocol.TxtRecord>()
                    .SelectMany(r => r.Text, (record, txt) => (record.DomainName.Value, txt))
                );
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
            _logger.LogDebug("TXT record was not of form <issuer>;<parameters>");
            return false;
        }

        var parameters = parameterStrings
            .Select(x => x.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length == 2)
            .ToLookup(x => x[0], x => x[1], StringComparer.OrdinalIgnoreCase);

        if (parameters[AccountParameter].Count() != 1)
        {
            _logger.LogDebug("TXT record did contain no or multiple accountUri parameters");
            return false;
        }

        if (parameters[PersistUntilParameter].Count() > 1)
        {
            _logger.LogDebug("TXT record did contain multiple persistUntil parameters");
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
                _logger.LogWarning("TXT record did contain persistUntil parameter, that could not be parsed as long");
                return false;
            }

            persistUntil = DateTimeOffset.FromUnixTimeSeconds(persistUntilUnixEpoch);
        }


        result = new()
        {
            DomainName = domainName,
            Issuer = issuer,
            AccountUri = accountUri,
            Policies = policies,
            ValidUntil = persistUntil
        };

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
}