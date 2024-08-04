using DnsClient;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation
{
    public sealed class Dns01ChallengeValidator : TokenChallengeValidator
    {
        private readonly ILogger<Dns01ChallengeValidator> _logger;

        public Dns01ChallengeValidator(ILogger<Dns01ChallengeValidator> logger)
            : base(logger)
        {
            _logger = logger;
        }

        protected override string GetExpectedContent(Challenge challenge, Account account) 
            => GetKeyAuthDigest(challenge, account);

        protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken)
        {
            var dnsBaseUrl = challenge.Authorization.Identifier.Value.Replace("*.", "", StringComparison.OrdinalIgnoreCase);
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
