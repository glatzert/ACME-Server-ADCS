using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Th11s.AcmeServer.Tests.AcmeClient;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services.ChallengeValidation.Tests.http_01;

public class Http01ValidatorTests : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly RsaSecurityKey _rsa;
    private readonly JsonWebKey _jsonWebKey;

    public Http01ValidatorTests()
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
    public async Task Http01_Generally_Works_With_DNS_Identifiers()
    {
        var httpClient = new HttpClient();
        var sut = new Http01ChallengeValidator(httpClient, NullLogger<Http01ChallengeValidator>.Instance);

        var account = new Account(
                new Jwk(_jsonWebKey.ExportPublicJwkJson()),
                ["example@th11s.de"],
                DateTimeOffset.UtcNow,
                null
            );

        var identifier = new Identifier("dns", "localhost:5000");

        var order = new Order(account.AccountId, [identifier]);

        var authZ = new Authorization(
            order, identifier,
            DateTimeOffset.Now.AddDays(1)
        );

        var challenge = new Challenge(
            authZ,
            "http-01"
        );

        var challengeContent = ChallengeValidator.GetKeyAuthToken(challenge, account);
        using var httpServer = new HttpServer("localhost", challengeContent);
        _ = httpServer.RunServer(_cts.Token);
        await httpServer.HasStarted;

        var result = await sut.ValidateChallengeAsync(challenge, account, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(httpServer.HasServedHttpToken);
        Assert.Equal(ChallengeResult.Valid, result.Result);
    }


    [Fact]
    public async Task Http01_Generally_Works_With_IP_Identifiers()
    {
        var httpClient = new HttpClient();
        var sut = new Http01ChallengeValidator(httpClient, NullLogger<Http01ChallengeValidator>.Instance);

        var account = new Account(
                new Jwk(_jsonWebKey.ExportPublicJwkJson()),
                ["example@th11s.de"],
                DateTimeOffset.UtcNow,
                null
            );

        var identifier = new Identifier("ip", "127.0.0.1:5000");

        var order = new Order(account.AccountId, [identifier]);

        var authZ = new Authorization(
            order, identifier,
            DateTimeOffset.Now.AddDays(1)
        );

        var challenge = new Challenge(
            authZ,
            "http-01"
        );

        var challengeContent = ChallengeValidator.GetKeyAuthToken(challenge, account);
        using var httpServer = new HttpServer("127.0.0.1", challengeContent);
        _ = httpServer.RunServer(_cts.Token);
        await httpServer.HasStarted;

        var result = await sut.ValidateChallengeAsync(challenge, account, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(httpServer.HasServedHttpToken);
        Assert.Equal(ChallengeResult.Valid, result.Result);
    }
}
