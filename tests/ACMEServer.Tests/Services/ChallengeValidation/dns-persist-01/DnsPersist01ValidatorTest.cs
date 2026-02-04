using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Th11s.ACMEServer.Tests.Utils;

namespace Th11s.ACMEServer.Tests.Services.ChallengeValidation.dns_persist_01;

public class DnsPersist01ValidatorTest : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    private static readonly AccountId _accountId = new();

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }


    [Theory,
        MemberData(nameof(DnsTxtRecordContentsData))]
    public async Task DnsPersist01_generally_works_with_DNS_identifiers(Dictionary<string, string[]> txtContents, bool shouldBeValid)
    {
        var lookupClient = new FakeLookupClient();
        foreach (var (name, content) in txtContents)
        {
            lookupClient.TxtRecords.Add(name, content);
        }

        var sut = new DnsPersist01ChallengeValidator(lookupClient, TimeProvider.System, NullLogger<DnsPersist01ChallengeValidator>.Instance) as IChallengeValidator;

        var account = new Account(
            _accountId,
            AccountStatus.Valid,
            new Jwk(JsonWebKeyConverter.ConvertFromRSASecurityKey(new(RSA.Create(2048))).ExportPublicJwkJson()),
            ["example@th11s.de"],
            DateTimeOffset.UtcNow,
            null,
            1L
        );

        var identifier = new Identifier(IdentifierTypes.DNS, "example.th11s.de");
        var order = new Order(_accountId, [identifier]);
        var authz = new Authorization(
            order, identifier,
            DateTimeOffset.Now.AddDays(1)
        );

        var challenge = new DnsPersistChallenge(
            authz,
            ["acme.th11s.de"]
        );

        var result = await sut.ValidateChallengeAsync(challenge, account, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(lookupClient.HasAnsweredQuestion);

        if (shouldBeValid)
            Assert.Equal(ChallengeResult.Valid, result.Result);
        else
            Assert.Equal(ChallengeResult.Invalid, result.Result);
    }

    // Data: TXT record contents mapped by record name, expected validity
    // TXT record should look like: <acme-caa-identity>;accountUri=<account-uri>;persistUntil=<unix-epoch-seconds>;policy=<policy1>...
    public static IEnumerable<object[]> DnsTxtRecordContentsData()
        => [
            [new Dictionary<string, string[]>() {
                ["example.th11s.de"] = [
                    $"acme.th11s.de;accountUri=https://acme.th11s.de/acct/{_accountId.Value};persistUntil=4105421562;policy=some-policy",
                ]
            }, true],

            [new Dictionary<string, string[]>() {
                ["example.th11s.de"] = [
                    $"acme.th11s.de;accountUri=https://acme.th11s.de/acct/{_accountId.Value};persistUntil=4105421562;policy=some-policy",
                    $"invalid.th11s.de;accountUri=https://acme.th11s.de/acct/{_accountId.Value};persistUntil=4105421562;policy=some-policy"
                ]
            }, true],

            [new Dictionary<string, string[]>() {
                ["th11s.de"] = [
                    $"acme.th11s.de;accountUri=https://acme.th11s.de/acct/{_accountId.Value};persistUntil=4105421562;policy=wildcard",
                ]
            }, true],
            
            // wrong DNS record name
            [new Dictionary<string, string[]>() {
                ["invalid.th11s.de"] = [
                    $"acme.th11s.de;accountUri=https://acme.th11s.de/acct/{_accountId.Value};persistUntil=4105421562;policy=some-policy",
                ]
            }, false],

            // invalid authority identity
            [new Dictionary<string, string[]>() {
                ["example.th11s.de"] = [
                    $"invalid.th11s.de;accountUri=https://acme.th11s.de/acct/{_accountId.Value};persistUntil=4105421562;policy=some-policy",
                ]
            }, false],
            
            // invalid account uri
            [new Dictionary<string, string[]>() {
                ["example.th11s.de"] = [
                    $"acme.th11s.de;accountUri=https://acme.th11s.de/acct/invalid;persistUntil=4105421562;policy=some-policy",
                ]
            }, false],
            
            // expired record
            [new Dictionary<string, string[]>() {
                ["example.th11s.de"] = [
                    $"acme.th11s.de;accountUri=https://acme.th11s.de/acct/invalid;persistUntil=0;policy=some-policy",
                ]
            }, false],

            // missing wildcard policy
            [new Dictionary<string, string[]>() {
                ["th11s.de"] = [
                    $"acme.th11s.de;accountUri=https://acme.th11s.de/acct/{_accountId.Value};persistUntil=4105421562;policy=some-policy",
                ]
            }, false],
        ];
}
