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

namespace Th11s.ACMEServer.Services.ChallangeValidation
{
    public sealed class Dns01ChallangeValidator : TokenChallengeValidator
    {
        private readonly ILogger<Dns01ChallangeValidator> _logger;

        public Dns01ChallangeValidator(ILogger<Dns01ChallangeValidator> logger)
            : base(logger)
        {
            _logger = logger;
        }

        protected override string GetExpectedContent(Challenge challenge, Account account)
        {
            using var sha256 = SHA256.Create();

            var thumbprintBytes = account.Jwk.SecurityKey.ComputeJwkThumbprint();
            var thumbprint = Base64UrlEncoder.Encode(thumbprintBytes);

            var keyAuthBytes = Encoding.UTF8.GetBytes($"{challenge.Token}.{thumbprint}");
            var digestBytes = sha256.ComputeHash(keyAuthBytes);

            var digest = Base64UrlEncoder.Encode(digestBytes);
            return digest;
        }

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
