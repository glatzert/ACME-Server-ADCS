using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Tests.Utils;

namespace Th11s.ACMEServer.Services.ChallengeValidation.Tests;

public class TlsAlpn01ValidatorTests : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly RsaSecurityKey _rsa;
    private readonly JsonWebKey _jsonWebKey;

    public TlsAlpn01ValidatorTests()
    {
        _rsa = new RsaSecurityKey(RSA.Create(2048));
        _jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(_rsa);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    [Fact]
    public async Task TlsAlpn01_Generally_Works()
    {
        var sut = new TlsAlpn01ChallengeValidator(NullLogger<TlsAlpn01ChallengeValidator>.Instance);

        var account = new Account(
                new Jwk(_jsonWebKey.ExportPublicJwkJson()),
                ["example@th11s.de"],
                DateTimeOffset.UtcNow,
                null
            );

        var identifier = new Identifier("dns", "localhost");

        var order = new Order(account.AccountId, [identifier]);

        var authZ = new Authorization(
            order, identifier,
            DateTimeOffset.Now.AddDays(1)
        );

        var challenge = new TokenChallenge(
            authZ,
            "tls-alpn-01"
        );

        var challengeContent = ChallengeValidator.GetKeyAuthDigest(challenge, account);
        using var tlsAlpnServer = new TlsAlpnServer("localhost", challengeContent);
        _ = tlsAlpnServer.RunServer(_cts.Token);

        var result = await sut.ValidateChallengeAsync(challenge, account, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(tlsAlpnServer.HasAuthorizedAsServer);
        Assert.Equal(ChallengeResult.Valid, result.Result);
    }
}