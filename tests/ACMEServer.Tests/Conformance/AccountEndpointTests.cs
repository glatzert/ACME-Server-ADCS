using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Tests.Integration;
using Th11s.ACMEServer.Tests.Utils;

namespace Th11s.ACMEServer.Tests.Conformance
{
    public class AccountEndpointTests
    : IClassFixture<DefaultWebApplicationFactory>
    {
        private readonly DefaultWebApplicationFactory _factory;

        public AccountEndpointTests(DefaultWebApplicationFactory factory)
        {
            _factory = factory;
        }


        private async Task<(HttpModel.Directory? directory, string? nonce)> GetDirectoryAndNonce(HttpClient client)
        {
            var directory = await client.GetFromJsonAsync<HttpModel.Directory>("/");

            if(directory?.NewNonce is null)
            {
                return (directory, null);
            }

            var nonceResponse = await client.GetAsync(directory.NewNonce);
            var nonce = nonceResponse.Headers.GetValues("Replay-Nonce").FirstOrDefault();

            return (directory, nonce);
        }


        [Fact]
        public async Task Account_Creation_yields_a_single_Location_Header()
        {
            // Arrange
            var client = _factory.CreateClient();
            var (directory, nonce) = await GetDirectoryAndNonce(client);

            Assert.NotNull(directory);
            Assert.NotNull(directory.NewAccount);
            Assert.NotNull(nonce);

            var requestUrl = new Uri(directory.NewAccount);
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(RSA.Create(2048)));

            var payload = new HttpModel.Payloads.CreateOrGetAccount() { Contact = ["hello@th11s.de"] };

            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                requestUrl);
            httpRequestMessage.CreateAcmeMessage(jwk, nonce, null, payload, []);

            // Act
            var response = await client.SendAsync(httpRequestMessage, TestContext.Current.CancellationToken);
            var locations = response.Headers.GetValues("Location").ToList();

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            Assert.Single(locations);
        }
    }
}
