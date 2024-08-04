using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;

namespace ACMEServer.Services.ChallengeValidation.Tests
{
    public class TlsAlpnServerFixture : IDisposable
    {
        internal JsonWebKey JsonWebKey { get; }
        CancellationTokenSource _cts = new();

        public TlsAlpnServerFixture()
        {
            var rsa = new RsaSecurityKey(RSA.Create(2048));
            JsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsa);

            _ = TlsAlpnServer.RunServer(_cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }

    
    public class UnitTest1 : IClassFixture<TlsAlpnServerFixture>
    {
        private readonly TlsAlpnServerFixture _fixture;

        public UnitTest1(TlsAlpnServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Test1()
        {
            var sut = new TlsAlpn01ChallengeValidator(NullLogger<TlsAlpn01ChallengeValidator>.Instance);

            var account = new Account(
                    new Jwk(_fixture.JsonWebKey.ExportPublicJwkJson()),
                    ["example@th11s.de"],
                    DateTimeOffset.UtcNow
                );

            var identifier = new Identifier("dns", "example.th11s.de");

            var order = new Order(account, [identifier]);

            var authZ = new Authorization(
                order, identifier,
                DateTimeOffset.Now.AddDays(1)
            );

            var challenge = new Challenge(
                authZ,
                "tls-alpn-01"
            );

            var result = await sut.ValidateChallengeAsync(challenge, account, CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(ChallengeResult.Valid, result.Result);
        }
    }
}