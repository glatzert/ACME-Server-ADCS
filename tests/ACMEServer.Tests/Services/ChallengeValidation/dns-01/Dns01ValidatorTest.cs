using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Th11s.ACMEServer.Tests.Utils;

namespace Th11s.ACMEServer.Tests.Services.ChallengeValidation.dns_01;

public class Dns01ValidatorTest : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private readonly RsaSecurityKey _rsa;
    private readonly JsonWebKey _jsonWebKey;

    public Dns01ValidatorTest()
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
    public async Task Dns01_generally_works_with_DNS_identifiers(Identifier identifier, bool shouldBeValid)
    {
        var lookupClient = new FakeLookupClient();
        var sut = new Dns01ChallengeValidator(lookupClient, NullLogger<Dns01ChallengeValidator>.Instance) as IChallengeValidator;

        var account = new Account(
            new Jwk(_jsonWebKey.ExportPublicJwkJson()),
            ["example@th11s.de"],
            DateTimeOffset.UtcNow,
            null
        );

        var order = new Order(account.AccountId, [identifier]);
        var authz = new Authorization(
            order, identifier, 
            DateTimeOffset.Now.AddDays(1)
        );

        var challenge = new TokenChallenge(
            authz,
            ChallengeTypes.Dns01
        );

        var challengeContent = Base64UrlEncoder.Encode(ChallengeValidator.GetKeyAuthDigest(challenge, account));
        if (shouldBeValid)
        {
            lookupClient.TxtRecords.Add(identifier.Value, [challengeContent, "something-else"]);
        }
        else
        {
            lookupClient.TxtRecords.Add(identifier.Value, ["invalid-content"]);
        }

        var result = await sut.ValidateChallengeAsync(challenge, account, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(lookupClient.HasAnsweredQuestion);

        if (shouldBeValid)
            Assert.Equal(ChallengeResult.Valid, result.Result);
        else
            Assert.Equal(ChallengeResult.Invalid, result.Result);
    }

    public static IEnumerable<object[]> IdentifierTestData()
        => [
            [new Identifier("dns", "th11s.de"), true],
            [new Identifier("dns", "th11s.de"), false]
        ];
}