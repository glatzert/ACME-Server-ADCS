using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Th11s.ACMEServer.Tests.Utils;
using Th11s.ACMEServer.Tests.Utils.Fakes;

namespace Th11s.ACMEServer.Tests.Services.ChallengeValidation.http_01;

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

    [Theory,
        MemberData(nameof(IdentifierTestData))]
    public async Task Http01_will_validate_for_Identifiers(Identifier identifier, bool shouldBeValid)
    {
        var profileProvider = new FakeProfileProvider(new() { { ProfileName.None, new() } });
        var sut = new Http01ChallengeValidator(new FakeHttpClientFactory(), profileProvider, NullLogger<Http01ChallengeValidator>.Instance);

        var account = new Account(
                new Jwk(_jsonWebKey.ExportPublicJwkJson()),
                ["example@th11s.de"],
                DateTimeOffset.UtcNow,
                null
            );

        var order = new Order(account.AccountId, [identifier])
        {
            Profile = ProfileName.None
        };

        var authZ = new Authorization(
            order, identifier,
            DateTimeOffset.Now.AddDays(1)
        );

        var challenge = new TokenChallenge(
            authZ,
            "http-01"
        );

        var challengeContent = ChallengeValidator.GetKeyAuthToken(challenge, account);
        using var httpServer = new HttpServer(identifier.Value, challengeContent + (shouldBeValid ? "" : "invalid"));
        _ = httpServer.RunServer(_cts.Token);
        await httpServer.HasStarted;

        var result = await sut.ValidateChallengeAsync(challenge, account, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(httpServer.HasServedHttpToken);

        if(shouldBeValid)
            Assert.Equal(ChallengeResult.Valid, result.Result);
        else
            Assert.Equal(ChallengeResult.Invalid, result.Result);
    }

    public static IEnumerable<object[]> IdentifierTestData()
        => [
            [new Identifier("dns", "localhost:5000"), true],
            [new Identifier("dns", "localhost:5000"), false],
            [new Identifier("dns", "127.0.0.1:5000"), true]
        ];
}
