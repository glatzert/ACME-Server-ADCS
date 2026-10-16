using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;
using Th11s.ACMEServer.Tests.Utils;
using Th11s.ACMEServer.Tests.Utils.Fakes;

namespace Th11s.ACMEServer.Tests.Services.ChallengeValidation.dns_persist_01;

public class DnsPersist01ValidatorTest
{
    private static readonly AccountId _accountId = new();
    private static readonly Account _account = new Account(
            _accountId,
            AccountStatus.Valid,
            new Jwk(JsonWebKeyConverter.ConvertFromRSASecurityKey(new (RSA.Create(2048))).ExportPublicJwkJson()),
            ["example@th11s.de"],
            DateTimeOffset.UtcNow,
            null,
            1L
        );


    [Theory,
        MemberData(nameof(DnsTxtRecordContentsData))]
    public async Task DnsPersist01_generally_works_with_DNS_identifiers(Dictionary<string, string[]> txtContents, bool shouldBeValid)
    {
        var lookupClient = new FakeLookupClient();
        foreach (var (name, content) in txtContents)
        {
            lookupClient.TxtRecords.Add(name, content);
        }

        var sut = new DnsPersist01ChallengeValidator(
            lookupClient,
            new FakeLinkGenerator(),
            TimeProvider.System, 
            NullLogger<DnsPersist01ChallengeValidator>.Instance) as IChallengeValidator;

        

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

        var result = await sut.ValidateChallengeAsync(challenge, _account, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(lookupClient.HasAnsweredQuestion);

        if (shouldBeValid)
            Assert.Equal(ChallengeResult.Valid, result.Result);
        else
            Assert.Equal(ChallengeResult.Invalid, result.Result);
    }

    // Data: TXT record contents mapped by record name, expected validity
    // TXT record should look like: <acme-caa-identity>;accountUri=<account-uri>;persistUntil=<unix-epoch-seconds>;policy=<policy1>...
    public static TheoryData<Dictionary<string, string[]>, bool> DnsTxtRecordContentsData()
        => new()
        {
            {
                new Dictionary<string, string[]>()
                {
                    ["example.th11s.de"] =
                    [
                        $"acme.th11s.de;accountUri={CalculateAccountUri()};persistUntil=4105421562;policy=some-policy",
                    ]
                },
                true
            },

            {
                new Dictionary<string, string[]>()
                {
                    ["example.th11s.de"] =
                    [
                        $"acme.th11s.de;accountUri={CalculateAccountUri()};persistUntil=4105421562;policy=some-policy",
                        $"invalid.th11s.de;accountUri={CalculateAccountUri()};persistUntil=4105421562;policy=some-policy"
                    ]
                },
                true
            },

            {
                new Dictionary<string, string[]>()
                {
                    ["th11s.de"] =
                    [
                        $"acme.th11s.de;accountUri={CalculateAccountUri()};persistUntil=4105421562;policy=wildcard",
                    ]
                },
                true
            },

            // wrong DNS record name
            {
                new Dictionary<string, string[]>()
                {
                    ["invalid.th11s.de"] =
                    [
                        $"acme.th11s.de;accountUri={CalculateAccountUri()};persistUntil=4105421562;policy=some-policy",
                    ]
                },
                false
            },

            // invalid authority identity
            {
                new Dictionary<string, string[]>()
                {
                    ["example.th11s.de"] =
                    [
                        $"invalid.th11s.de;accountUri={CalculateAccountUri()};persistUntil=4105421562;policy=some-policy",
                    ]
                },
                false
            },

            // invalid account uri
            {
                new Dictionary<string, string[]>()
                {
                    ["example.th11s.de"] =
                    [
                        $"acme.th11s.de;accountUri={CalculateAccountUri()}/invalid;persistUntil=4105421562;policy=some-policy",
                    ]
                },
                false
            },

            // expired record
            {
                new Dictionary<string, string[]>()
                {
                    ["example.th11s.de"] =
                    [
                        $"acme.th11s.de;accountUri={CalculateAccountUri()};persistUntil=0;policy=some-policy",
                    ]
                },
                false
            },

            // missing wildcard policy
            {
                new Dictionary<string, string[]>()
                {
                    ["th11s.de"] =
                    [
                        $"acme.th11s.de;accountUri={CalculateAccountUri()};persistUntil=4105421562;policy=some-policy",
                    ]
                },
                false
            },

            // fancy casing and whitespace
            {
                new Dictionary<string, string[]>()
                {
                    ["example.th11s.de"] =
                    [
                        $"  AcMe.Th11s.De  ;  aCcOuNtUrI = {CalculateAccountUri()}  ;  persist = 4105421562  ;  policy = some-policy  ",
                    ]
                },
                true
            },
        };

    private static string CalculateAccountUri()
    {
        var domainName = "example.th11s.de";

        // https://datatracker.ietf.org/doc/html/draft-ietf-acme-dns-persist-02#section-4.1
        byte[] data = [
            (byte)domainName.Length,
            .. Encoding.ASCII.GetBytes(domainName),
            .. Encoding.ASCII.GetBytes(_account.Jwk.KeyHash),
            .. Encoding.ASCII.GetBytes(new FakeLinkGenerator().GetAccount(_account.AccountId))
        ];

        return Base64UrlEncoder.Encode(SHA256.HashData(data));
    }
}