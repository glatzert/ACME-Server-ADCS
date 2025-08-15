using DnsClient;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation
{
    public sealed class Dns01ChallengeValidator : StringTokenChallengeValidator
    {
        private readonly ILogger<Dns01ChallengeValidator> _logger;

        public Dns01ChallengeValidator(ILogger<Dns01ChallengeValidator> logger)
            : base(logger)
        {
            _logger = logger;
        }

        public override string ChallengeType => ChallengeTypes.Dns01;

        protected override string GetExpectedContent(Challenge challenge, Account account)
            => Base64UrlEncoder.Encode(GetKeyAuthDigest(challenge, account));
            

        protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken)
        {
            var dnsBaseUrl = challenge.Authorization.Identifier.Value;
            var dnsRecordName = $"_acme-challenge.{dnsBaseUrl}";

            try
            {
                var dnsClient = new LookupClient();

                var dnsResponse = await dnsClient.QueryAsync(dnsRecordName, QueryType.TXT, cancellationToken: cancellationToken);
                var contents = new List<string>(dnsResponse.Answers.TxtRecords().SelectMany(x => x.Text));

                _logger.LogInformation($"Loaded dns-01 challenge response from {dnsRecordName}: {string.Join(";", contents)}");
                return (contents, null);
            }
            catch (DnsResponseException)
            {
                _logger.LogInformation($"Could not load dns-01 challenge response from {dnsRecordName}");
                return (null, new AcmeError("dns", "Could not read from DNS"));
            }
        }
    }
}
