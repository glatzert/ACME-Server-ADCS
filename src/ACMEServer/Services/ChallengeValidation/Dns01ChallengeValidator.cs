using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

/// <summary>
/// Implements challenge validation as described in the ACME RFC 8555 (https://www.rfc-editor.org/rfc/rfc8555#section-8.4) for the "dns-01" challenge type.
/// </summary>
public sealed class Dns01ChallengeValidator(
    [FromKeyedServices(nameof(Dns01ChallengeValidator))] ILookupClient lookupClient,
    ILogger<Dns01ChallengeValidator> logger
) : StringTokenChallengeValidator(logger)
{
    private readonly ILookupClient _lookupClient = lookupClient;
    private readonly ILogger<Dns01ChallengeValidator> _logger = logger;

    public override string ChallengeType => ChallengeTypes.Dns01;
    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.DNS];

    protected override string GetExpectedContent(TokenChallenge challenge, Account account)
        => Base64UrlEncoder.Encode(GetKeyAuthDigest(challenge, account));
        

        protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(TokenChallenge challenge, CancellationToken cancellationToken)
        {
            var dnsBaseUrl = challenge.Authorization.Identifier.Value;
            var dnsRecordName = $"_acme-challenge.{dnsBaseUrl}";

        try
        {
            var dnsResponse = await _lookupClient.QueryAsync(dnsRecordName, QueryType.TXT, cancellationToken: cancellationToken);
            var contents = new List<string>(dnsResponse.Answers.TxtRecords().SelectMany(x => x.Text));

            _logger.Dns01ChallengeResponseLoaded(dnsRecordName, string.Join(";", contents));
            return (contents, null);
        }
        catch (DnsResponseException)
        {
            _logger.Dns01ChallengeResponseFailed(dnsRecordName);
            return (null, AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "Could not read from DNS"));
        }
    }
}
