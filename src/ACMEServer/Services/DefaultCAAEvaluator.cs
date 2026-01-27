using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.CAA;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.Services;

public class DefaultCAAEvaluator(
    ICAAQueryHandler caaQueryHandler, 
    IOptions<ACMEServerOptions> options,
    ILogger<DefaultCAAEvaluator> logger
    ) : ICAAEvaluator
{
    private readonly ICAAQueryHandler _caaQueryHandler = caaQueryHandler;

    private readonly IOptions<ACMEServerOptions> _options = options;
    private readonly ILogger<DefaultCAAEvaluator> _logger = logger;

    private static readonly string[] _knownParameters = [CAAACMEParameters.AccountURI, CAAACMEParameters.ValidationMethods];
    private static readonly string[] _challengeTypes = [ChallengeTypes.DeviceAttest01, ChallengeTypes.Http01, ChallengeTypes.Dns01, ChallengeTypes.TlsAlpn01];

    public async Task<CAAEvaluationResult> EvaluateCAA(CAAEvaluationContext evaluationContext, CancellationToken cancellationToken)
    {
        var caaEntries = await _caaQueryHandler.GetCAAFromDomain(evaluationContext.Identifier.Value, cancellationToken);

        // If CAA does not exist, we're allowed to issue certificates
        if (caaEntries.Count == 0) {
            _logger.LogDebug("No CAA entries were present for {identifier}. Issuance is allowed.", evaluationContext.Identifier);
            return CAAEvaluationResult.IssuanceAllowed;
        }


        if (evaluationContext.Identifier.Type == IdentifierTypes.DNS)
        {
            // Parameters can be spread across multiple entries OR be contained in a single one, so we concat them
            var issueEntries = caaEntries
                .Where(x => x.Tag == CAATags.Issue || x.Tag == CAATags.IssueWild)
                .GroupBy(x => (x.Tag, x.CAIdentifier, x.Flags))
                .Select(x => new CAAQueryResult()
                {
                    Tag = x.Key.Tag,
                    CAIdentifier = x.Key.CAIdentifier,
                    Flags = x.Key.Flags,
                    Parameters = [.. x.SelectMany(x => x.Parameters)]
                })
                .ToList();

            return EvaluateDNSCAA(evaluationContext, issueEntries);
        }

        if (evaluationContext.Identifier.Type == IdentifierTypes.Email)
        {
            // TODO: Implement CAA checking for Email identifiers when we add support for them
            var issueMailTags = caaEntries.Where(x => x.Tag == CAATags.IssueMail);
            throw new NotImplementedException();
        }

        // Identifier is not DNS or Email, so we assume CAA is not applicable
        return CAAEvaluationResult.IssuanceAllowed;
    }

    private CAAEvaluationResult EvaluateDNSCAA(CAAEvaluationContext evaluationContext, IList<CAAQueryResult> caaIssueAndIssueWildEntries)
    {
        // If we got here, we need to have an CAAIdentifier
        if (_options.Value.CAAIdentities.Length == 0)
        {
            _logger.LogWarning("CAA evaluation was requested, but no CAA identities are configured. CAA evaluation failed for {Identifier}.", evaluationContext.Identifier);
            return CAAEvaluationResult.IssuanceForbidden;
        }


        // Determine effective entries based on whether the identifier is a wildcard or not
        CAAQueryResult[] effectiveEntries = [];
        if (evaluationContext.Identifier.IsWildcard())
        {
            effectiveEntries = caaIssueAndIssueWildEntries
                .Where(x => x.Tag == CAATags.IssueWild)
                .ToArray();
        }

        // If no effective entries are present now, the identifier is either not a wildcard or there were no issueWild entries
        if (effectiveEntries.Length == 0)
        {
            effectiveEntries = caaIssueAndIssueWildEntries
                .Where(x => x.Tag == CAATags.Issue)
                .ToArray();
        }

        // Now we filter the effective entries to those matching our CAA identities
        var matchingCAAEntries = effectiveEntries
            .Where(x => _options.Value.CAAIdentities.Contains(x.CAIdentifier, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (matchingCAAEntries.Length == 0)
        {
            _logger.LogWarning("No CAA entry matched our CAA identifiers. CAA evaluation failed for {Identifier}", evaluationContext.Identifier);
            return CAAEvaluationResult.IssuanceForbidden;
        }


        // If we got here, we have matching CAA entries. We need to evaluate their parameters now

        List<string> accountUris = [];
        List<string> validationMethods = [];

        foreach (var entry in matchingCAAEntries)
        {
            var understoodParameters = TryReadCAAACMEParameters(entry.Parameters, out var entryAccountUris, out var entryValidationMethods);
            if(!understoodParameters && entry.Flags == CAAFlags.IssuerCritical)
            {
                return CAAEvaluationResult.IssuanceForbidden;
            }

            accountUris.AddRange(entryAccountUris);
            validationMethods.AddRange(entryValidationMethods);
        }

        // AccountUris will look like: "https://acme.th11s.de/account/<accountId>" and since accountId is a UUID, we can just check the ending of the URI
        if (accountUris.Any(x => !x.EndsWith($"/{evaluationContext.AccountId.Value}", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("CAA AccountURI parameters did not match the requesting account. CAA evaluation failed for {Identifier} and AccountId {AccountId}", evaluationContext.Identifier, evaluationContext.AccountId);
            return CAAEvaluationResult.IssuanceForbidden;
        }

        // TODO! validation methods will be added to identifier metadata
        // currently it's that way, but it could be moved to the evaluation context and then directly used in authorization factories
        if (validationMethods.Count > 0)
        {
            _logger.LogInformation("Found validationMethods requirements in CAA, that will be written to identifier metadata: {validationMethods}", string.Join(", ", validationMethods));
            evaluationContext.Identifier.Metadata[Identifier.MetadataKeys.CAAValidationMehods] = JsonSerializer.Serialize(validationMethods.Distinct());
        }

        return CAAEvaluationResult.IssuanceAllowed;
    }

    private bool TryReadCAAACMEParameters(string[] parameters, out List<string> accountUris, out List<string> validationMethods)
    {
        accountUris = new List<string>();
        validationMethods = new List<string>();
        var understoodAllParameters = true;

        foreach(var p in parameters)
        {
            if(p.Split('=',2) is not [var parameterKey, var parameterValue])
            {
                _logger.LogInformation("Could not understand parameter with value {p}", p);
                understoodAllParameters = false;
                continue;
            }

            if(!_knownParameters.Contains(parameterKey))
            {
                _logger.LogInformation("Parameter {key} was not a known parameter", parameterKey);
                understoodAllParameters = false;
                continue;
            }

            var values = parameterValue.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (parameterKey == CAAACMEParameters.AccountURI)
            {
                accountUris.AddRange(values);
            }
            else if (parameterKey == CAAACMEParameters.ValidationMethods)
            {
                foreach(var v in values)
                {
                    if(!_challengeTypes.Contains(v))
                    {
                        understoodAllParameters = false;
                        continue;
                    }

                    validationMethods.Add(v);
                }
            }
        }

        return understoodAllParameters;
    }
}
