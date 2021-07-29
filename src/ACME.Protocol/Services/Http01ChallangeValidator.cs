using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public sealed class Http01ChallangeValidator : TokenChallengeValidator
    {
        private readonly HttpClient _httpClient;

        public Http01ChallangeValidator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected override string GetExpectedContent(Challenge challenge, Account account)
        {
            var thumbprintBytes = account.Jwk.SecurityKey.ComputeJwkThumbprint();
            var thumbprint = Base64UrlEncoder.Encode(thumbprintBytes);

            var expectedContent = $"{challenge.Token}.{thumbprint}";
            return expectedContent;
        }

        protected override async Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken)
        {
            var challengeUrl = $"http://{challenge.Authorization.Identifier.Value}/.well-known/acme-challenge/{challenge.Token}";

            try
            {
                var response = await _httpClient.GetAsync(new Uri(challengeUrl), cancellationToken);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    var error = new AcmeError("incorrectResponse", $"Got non 200 status code: {response.StatusCode}", challenge.Authorization.Identifier);
                    return (null, error);
                }

                var content = await response.Content.ReadAsStringAsync();
                return (new List<string> { content }, null);
            } 
            catch (HttpRequestException ex)
            {
                var error = new AcmeError("connection", ex.Message, challenge.Authorization.Identifier);
                return (null, error);
            }
        }
    }
}
