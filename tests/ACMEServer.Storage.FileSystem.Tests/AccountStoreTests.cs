using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Tests.Utils;

namespace ACMEServer.Storage.FileSystem.Tests;

public class AccountStoreTests : StoreTestBase
{
    [Fact]
    public async Task Saving_an_Account_Creates_Account_File_And_JWK_Index_File()
    {
        var jwk = JsonWebKeyFactory.CreateRsaJsonWebKey().ToAcmePublicJwk();

        var account = new Account(jwk, ["mailto:some@th11s.de"], DateTimeOffset.Now, null);

        var sut = new AccountStore(new OptionsWrapper<FileStoreOptions>(Options));
        await sut.SaveAccountAsync(account, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, account.AccountId.Value, "account.json")));
        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, account.Jwk.KeyHash)));
    }

    [Fact]
    public async Task Saved_Accounts_Can_Be_Found_And_Loaded()
    {
        var account = new Account(
            new AccountId(),
            AccountStatus.Revoked,
            JsonWebKeyFactory.CreateECDsaJsonWebKey().ToAcmePublicJwk(),
            ["mailto:some@th11s.de", "mailto:another@th11s.de"],
            DateTimeOffset.UtcNow,
            new(
                Base64UrlEncoder.Encode("""{"Alg": "ES256"}"""),
                Base64UrlEncoder.Encode("payload"), 
                Base64UrlEncoder.Encode("signature")
            ),
            Random.Shared.NextInt64()
        );

        var sut = new AccountStore(new OptionsWrapper<FileStoreOptions>(Options));
        await sut.SaveAccountAsync(account, CancellationToken.None);

        var foundAccount = await sut.FindAccountAsync(account.Jwk, CancellationToken.None);
        Assert.NotNull(foundAccount);
        
        Assert.Equal(account.AccountId, foundAccount.AccountId);
        Assert.Equal(account.Status, foundAccount.Status);
        Assert.Equal(account.Jwk.KeyHash, foundAccount.Jwk.KeyHash);
        Assert.Equal(account.Contacts, foundAccount.Contacts);
        Assert.Equal(account.TOSAccepted, foundAccount.TOSAccepted);
        Assert.Equal(account.ExternalAccountBinding?.Protected, foundAccount.ExternalAccountBinding?.Protected);
        Assert.Equal(account.ExternalAccountBinding?.Payload, foundAccount.ExternalAccountBinding?.Payload);
        Assert.Equal(account.ExternalAccountBinding?.Signature, foundAccount.ExternalAccountBinding?.Signature);
        Assert.Equal(account.Version, foundAccount.Version);
    }

    [Theory,
        InlineData(AccountJsonFileVariants.Account_SV2_FullModel_ECDSA),
        InlineData(AccountJsonFileVariants.Account_SV1_FullModel_RSA),
        InlineData(AccountJsonFileVariants.Account_SV1_FullModel_ECDSA),
        InlineData(AccountJsonFileVariants.Account_SV1_MinimalModel)]
    public async Task Exisiting_File_Variants_Can_Be_Loaded(string accountJson)
    {
        var accountId = new AccountId("pqyaNlRFVUakjv8Rr36I3Q");
        var accountFilePath = Path.Combine(Options.AccountDirectory, accountId.Value, "account.json");
        Directory.CreateDirectory(Path.GetDirectoryName(accountFilePath)!);

        await File.WriteAllTextAsync(accountFilePath, accountJson, TestContext.Current.CancellationToken);

        var sut = new AccountStore(new OptionsWrapper<FileStoreOptions>(Options));
        var loadedAccount = await sut.LoadAccountAsync(accountId, TestContext.Current.CancellationToken);

        Assert.NotNull(loadedAccount);
        Assert.Equal("SmB_jts3zkK0wUNHyfcO8g", loadedAccount.AccountId.Value);
        Assert.Equal(AccountStatus.Revoked, loadedAccount.Status);
    }
}

internal static class AccountJsonFileVariants {
    public const string Account_SV1_FullModel_RSA = """
    {
        "$id": "1",
        "SerializationVersion": 1,
        "AccountId": "SmB_jts3zkK0wUNHyfcO8g",
        "Status": "Revoked",
        "Jwk": {
            "$id": "2",
            "Json": "{\"e\":\"AQAB\",\"kty\":\"RSA\",\"n\":\"ouKB-Y5bMg6-KLsoso4k-G3cpmIMzqshwT9_Zv9k8VnTWXSHl5Vc53rUmQ5j0h6k-cfZhbH7eNi98gMKnEhLfgJC5_WUMcb9SKzeh0vZ1uL6N7tEZLHFCc-kSGcaGG7kwr8o6CzPOXMqf-2caH5sJdLlQwke2GSDXRkRj8FEPj74dtBRsbeYMFsPv2wlLtX9jXcDfZ-ZqFVSnkMXpq6THocA5DTV45Y7ovU8lPEx6LxzTQnk0l0vx7QCqYmPDO80nD1EPrhJfjfsMqpclSKmnOCjx501b0BtF2uL0OlTLzs8wj1C9oEPYZ04lUsfMpMLwxVYdSsyIWcEOQdKZhyvfQ\"}"
        },
        "Contacts": {
            "$id": "3",
            "$values": [
                "mailto:some@th11s.de",
                "mailto:another@th11s.de"
            ]
        },
        "TOSAccepted": "2026-01-25T08:36:17.8406509+00:00",
        "ExternalAccountBinding": {
            "$id": "4",
            "SerializationVersion": 1,
            "Protected": "eyJBbGciOiAiUlMyNTYifQ",
            "Payload": "cGF5bG9hZA",
            "Signature": "c2lnbmF0dXJl"
        },
        "Version": 639049268140604754
    }
    """;

    public const string Account_SV1_FullModel_ECDSA = """
    {
        "$id": "1",
        "SerializationVersion": 1,
        "AccountId": "SmB_jts3zkK0wUNHyfcO8g",
        "Status": "Revoked",
        "Jwk": {
            "$id": "2",
            "Json": "{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"niySKW8r4HwTUKwID6-eiE-BbARo6iNaBsHsoEw7V-I\",\"y\":\"265N1QFUK8oyRDWcl0ULVIKuRWdNRKqehRjKhiZSObc\"}"
        },
        "Contacts": {
            "$id": "3",
            "$values": [
                "mailto:some@th11s.de",
                "mailto:another@th11s.de"
            ]
        },
        "TOSAccepted": "2026-01-25T08:36:17.8406509+00:00",
        "ExternalAccountBinding": {
            "$id": "4",
            "SerializationVersion": 1,
            "Protected": "eyJBbGciOiAiRVMyNTYifQ",
            "Payload": "cGF5bG9hZA",
            "Signature": "c2lnbmF0dXJl"
        },
        "Version": 639049268140604754
    }
    """;

    public const string Account_SV1_MinimalModel = """
    {
        "$id": "1",
        "SerializationVersion": 1,
        "AccountId": "SmB_jts3zkK0wUNHyfcO8g",
        "Status": "Revoked",
        "Jwk": {
            "$id": "2",
            "Json": "{\"e\":\"AQAB\",\"kty\":\"RSA\",\"n\":\"ouKB-Y5bMg6-KLsoso4k-G3cpmIMzqshwT9_Zv9k8VnTWXSHl5Vc53rUmQ5j0h6k-cfZhbH7eNi98gMKnEhLfgJC5_WUMcb9SKzeh0vZ1uL6N7tEZLHFCc-kSGcaGG7kwr8o6CzPOXMqf-2caH5sJdLlQwke2GSDXRkRj8FEPj74dtBRsbeYMFsPv2wlLtX9jXcDfZ-ZqFVSnkMXpq6THocA5DTV45Y7ovU8lPEx6LxzTQnk0l0vx7QCqYmPDO80nD1EPrhJfjfsMqpclSKmnOCjx501b0BtF2uL0OlTLzs8wj1C9oEPYZ04lUsfMpMLwxVYdSsyIWcEOQdKZhyvfQ\"}"
        },
        "Contacts": {
            "$id": "3",
            "$values": [
                "mailto:some@th11s.de",
                "mailto:another@th11s.de"
            ]
        },
        "Version": 639049268140604754
    }
    """;

    public const string Account_SV2_FullModel_ECDSA = """
    {
      "SerializationVersion": 2,
      "AccountId": "SmB_jts3zkK0wUNHyfcO8g",
      "Status": "Revoked",
      "Jwk": "{\u0022crv\u0022:\u0022P-256\u0022,\u0022kty\u0022:\u0022EC\u0022,\u0022x\u0022:\u0022IKqt1AVp3ZjHbS8td02vkptxTx8Eecvub-AcblA9xCU\u0022,\u0022y\u0022:\u002258yrulEV0UQhqxJXgw0GJI970i2SVwc0Dylp2Qjogrk\u0022}",
      "Contacts": [
        "mailto:some@th11s.de",
        "mailto:another@th11s.de"
      ],
      "TOSAccepted": "2026-01-25T08:36:17.8406509+00:00",
      "ExternalAccountBinding": {
        "Protected": "eyJBbGciOiAiRVMyNTYifQ",
        "Payload": "cGF5bG9hZA",
        "Signature": "c2lnbmF0dXJl"
      },
      "Version": 639049724706829412
    }
    """;
}
