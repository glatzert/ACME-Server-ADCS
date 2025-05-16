using DnsClient;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

public sealed class Dns01ChallengeValidator(ILogger<Dns01ChallengeValidator> logger) : StringTokenChallengeValidator(logger)
{
    private readonly ILogger<Dns01ChallengeValidator> _logger = logger;

    public override string ChallengeType => ChallengeTypes.Dns01;
    public override IEnumerable<string> SupportedIdentiferTypes => [IdentifierTypes.DNS];

    protected override string GetExpectedContent(Challenge challenge, Account account)
        => Base64UrlEncoder.Encode(GetKeyAuthDigest(challenge, account));
        

    protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken)
    {
        var dnsBaseUrl = challenge.Authorization.Identifier.Value.Replace("*.", "", StringComparison.OrdinalIgnoreCase);
        var dnsRecordName = $"_acme-challenge.{dnsBaseUrl}";

        try
        {
            // TODO: Use a "trusted DNS Resolver" configuration to avoid DNS spoofing attacks.
            var dnsClient = new LookupClient();

            var dnsResponse = await dnsClient.QueryAsync(dnsRecordName, QueryType.TXT, cancellationToken: cancellationToken);
            var contents = new List<string>(dnsResponse.Answers.TxtRecords().SelectMany(x => x.Text));

            _logger.LogInformation("Loaded dns-01 challenge response from {dnsRecordName}: {contents}", dnsRecordName, string.Join(";", contents));
            return (contents, null);
        }
        catch (DnsResponseException)
        {
            _logger.LogInformation("Could not load dns-01 challenge response from {dnsRecordName}", dnsRecordName);
            return (null, AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "Could not read from DNS"));
        }
    }
}
