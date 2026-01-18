using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace ACMEServer.Storage.FileSystem.Tests;

public class OrderStoreTests : StoreTestBase
{
    [Fact]
    public async Task Saving_an_Order_Creates_Order_File_and_Reference_File()
    {
        var order = new Order(new(), [new(IdentifierTypes.DNS, "example.th11s.de")]);

        var sut = new OrderStore(new OptionsWrapper<FileStoreOptions>(Options), NullLogger<OrderStore>.Instance);
        await sut.SaveOrderAsync(order, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Options.OrderDirectory, $"{order.OrderId.Value}.json")), "Order was not saved at expected path.");
        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, order.AccountId.Value, "orders", order.OrderId.Value)), "Reference File was not saved at expected path.");
    }

    [Fact]
    public async Task Saved_Orders_Can_Be_Loaded()
    {
        var identifiers = new List<Identifier>
        {
            new(IdentifierTypes.DNS, "example.th11s.de"),
            new(IdentifierTypes.HardwareModule, "hardware-module")
        };

        var order = new Order(
            new(),
            new(),
            OrderStatus.Invalid,
            identifiers,
            [
                new(
                    new(),
                    AuthorizationStatus.Valid,
                    identifiers[0],
                    true,
                    DateTimeOffset.UtcNow.AddDays(5),
                    [
                        new DeviceAttestChallenge(
                            new(), 
                            ChallengeStatus.Valid,
                            "device-attest-01",
                            "token",
                            "payload",
                            DateTimeOffset.Now.AddMinutes(-1),
                            null
                        ),
                        new TokenChallenge(
                            new(),
                            ChallengeStatus.Invalid,
                            "http-01",
                            "token",
                            DateTimeOffset.Now.AddMinutes(-1),
                            new("test:challengeError", "SomeError") { Identifier = identifiers[0] }
                        )
                    ]
                ),
                new(
                    new(),
                    AuthorizationStatus.Deactivated,
                    identifiers[1],
                    false,
                    DateTimeOffset.Now.AddDays(3),
                    [
                        new DeviceAttestChallenge(
                            new(),
                            ChallengeStatus.Invalid,
                            ChallengeTypes.DeviceAttest01,
                            "token",
                            "payload",
                            null,
                            null
                        )
                    ]
                )
            ],
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow.AddDays(20),
            new("profile"),
            "MIIDqzCCAhMCAQAwHjEcMBoGA1UEAwwTc2l0ZS53ZWIudGgxMXMuY29ycDCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAIhl5YrTTonUAGaqx82pbFMpnggvSbGX444t16EnlToT3IV6P-RjQSgOYjnkC8xcTmcJs-ZTSYcDiI310im485ZzfUBmkBT2wXqXHn9g7n3E0GyCVx4LVf8lSbJZIEyzKvLINNpjKu4bAel6vAiDHnOkHM_ySnCLVnzt9Tc4hTxQvrLvs7bUO1M7Tpt5hrV29dCQRreuvPfa10CjVoWOYiz2ufb-KVDvoK4qIqq7M0zWdjs-hRnKdKX2MmI8L3NiNWVTUlTn4YuCE9003Fma1N-bRfpuUbi3-goXOtQx2QN9sVDCIfVQQzN7cL-N9MkzuMNvgEy3w-XoNFtz_-XzH0St8rqSu0SSnLaycYcgPPnj97QUkTR-Du5Oj-EzBU4KMY2XJgwvikzDlWtAQGl5z7sLIpSCgvCd4VLs9TmTbQ4yHMUn0lHd3Ek_JDyFViwsyNISmDSFFoH1w0ql7_jpK8RU0V8zleR-0aEOjpZ6pVt9r20ATtEygAx47fV2UJXGmQIDAQABoEgwRgYJKoZIhvcNAQkOMTkwNzA1BgNVHREELjAsghNzaXRlLndlYi50aDExcy5jb3JwghVzaXRlLWEud2ViLnRoMTFzLmNvcnAwDQYJKoZIhvcNAQENBQADggGBAEMR_LKFhWYHpzW4hjpv35ehSLZL-ii2M_1p5s5oiig59g0KtjiXmr3WSPRlMS072vxMjLDx3VWBmND7dmu7CWnxPPLM9Toi0kY4kw6uknJHQfzeF2e_mC0NGjJPO0zc1fX3Bphn3qERsr_GiOVI7poSNOpQuBeVJwR-Tbk9wxPW21Kxct-3jQn25ok11olWFKUqO3kjaSqm0AJOpXKKq5woFeDRMRZicNn9mje4Ci4PLDX-EdIaWJ2o5LdaacNEBj1ylVAMMiLIx3aYBkRLjuSUvNqSkibJBXUDANiWq4uprIr5L9VA2g09twXi4_kxhqtGZHzzJrC8Q4wokuLIKsR6DFOEAFGAtaeT_jf72Pqu8kA9riY82ZGbTeVJK2Z2ftgNesnf2VkwJuFKUpFmVdSedxDIlEeIXXIJojlAfUgxdFv18SXcdXjdsXMJ9V_nzcI1lyPnUgelpssvpNwor6MvBDJxkHdseoNxs_YnY5xrIU4p91URa44nB-m4gz2Q-Q",
            new(),
            "expected-public-key",
            new(
                "test:orderError",
                "This is a test", 
                [
                    new("test:orderSubError", "This is a test suberror")
                ])
            {
                HttpStatusCode = 42,
                Identifier = identifiers[1],
                AdditionalFields =
                {
                    ["Algorithms"] = new string[] { "RS256", "ES256" }
                }
            },

            Random.Shared.NextInt64()
        );

        var sut = new OrderStore(new OptionsWrapper<FileStoreOptions>(Options), NullLogger<OrderStore>.Instance);
        await sut.SaveOrderAsync(order, CancellationToken.None);

        var loadedOrder = await sut.LoadOrderAsync(order.OrderId, CancellationToken.None);

        Assert.NotNull(loadedOrder);

            Assert.Equal(order.OrderId, loadedOrder.OrderId);
            Assert.Equal(order.AccountId, loadedOrder.AccountId);
        
            Assert.Equal(order.Status, loadedOrder.Status);

        for(int i = 0; i < order.Identifiers.Count; i++)
        {
            var expected = order.Identifiers[i];
            var actual = loadedOrder.Identifiers[i];

            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.Value, actual.Value);
        }
        
        for(int i = 0; i < order.Authorizations.Count; i++)
        {
            var expected = order.Authorizations[i];
            var actual = loadedOrder.Authorizations[i];
            Assert.Equal(expected.AuthorizationId, actual.AuthorizationId);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.Identifier.Type, actual.Identifier.Type);
            Assert.Equal(expected.Identifier.Value, actual.Identifier.Value);
            Assert.Equal(expected.IsWildcard, actual.IsWildcard);
            Assert.Equal(expected.Expires, actual.Expires);

            for (int j = 0; j < expected.Challenges.Count; j++)
            {
                var expectedChallenge = expected.Challenges[j];
                var actualChallenge = actual.Challenges[j];
                Assert.Equal(expectedChallenge.ChallengeId, actualChallenge.ChallengeId);
                Assert.Equal(expectedChallenge.Status, actualChallenge.Status);
                Assert.Equal(expectedChallenge.Type, actualChallenge.Type);

                if(expectedChallenge is TokenChallenge expectedTokenChallenge)
                {
                    var actualTokenChallenge = Assert.IsAssignableFrom<TokenChallenge>(actualChallenge);

                    Assert.Equal(expectedTokenChallenge.Token, actualTokenChallenge.Token);

                    if (expectedChallenge is DeviceAttestChallenge expectedDeviceAttestChallenge)
                    {
                        var actualDeviceAttestChallenge = Assert.IsType<DeviceAttestChallenge>(actualChallenge);
                        Assert.Equal(expectedDeviceAttestChallenge.Payload, actualDeviceAttestChallenge.Payload);
                    }
                }
                
                Assert.Equal(expectedChallenge.Validated, actualChallenge.Validated);
                Assert.Equivalent(expectedChallenge.Error, actualChallenge.Error, strict: true);
            }
        }

        Assert.Equal(order.NotBefore, loadedOrder.NotBefore);
            Assert.Equal(order.NotAfter, loadedOrder.NotAfter);
        Assert.Equal(order.Expires, loadedOrder.Expires);

            Assert.Equal(order.Profile, loadedOrder.Profile);
        Assert.Equal(order.CertificateSigningRequest, loadedOrder.CertificateSigningRequest);

        Assert.Equal(order.ExpectedPublicKey, loadedOrder.ExpectedPublicKey);

        Assert.Equivalent(order.Error, loadedOrder.Error, strict: true);
            Assert.Equal(order.Version, loadedOrder.Version);
        }

    [Theory,
        InlineData(OrderJsonFileVariants.Order_SV1_FullModel)]
    public async Task Existing_File_Variants_Can_Be_Loaded(string orderJson)
    {
        Directory.CreateDirectory(Options.OrderDirectory);

        var orderId = new OrderId("pqyaNlRFVUakjv8Rr36I3Q");
        var orderFilePath = Path.Combine(Options.OrderDirectory, $"{orderId.Value}.json");
        await File.WriteAllTextAsync(orderFilePath, orderJson, TestContext.Current.CancellationToken);

        var sut = new OrderStore(new OptionsWrapper<FileStoreOptions>(Options), NullLogger<OrderStore>.Instance);
        var loadedOrder = await sut.LoadOrderAsync(orderId, TestContext.Current.CancellationToken);

        Assert.NotNull(loadedOrder);

        if (loadedOrder != null)
        {
            Assert.Equal(orderId, loadedOrder.OrderId);
            Assert.Equal("Y47V6u5uoEu-GNAuWaQ16w", loadedOrder.AccountId.Value);
            Assert.Equal(OrderStatus.Valid, loadedOrder.Status);
            Assert.Equal(DateTimeOffset.Parse("2027-11-27T16:34:11+01:00"), loadedOrder.Expires);

            Assert.Equal(2, loadedOrder.Identifiers.Count);
            Assert.Equal(2, loadedOrder.Authorizations.Count);
            Assert.Equal(loadedOrder.Authorizations.FirstOrDefault()?.Identifier, loadedOrder.Identifiers.FirstOrDefault());

            Assert.Null(loadedOrder.NotAfter);
            Assert.Null(loadedOrder.NotBefore);
            Assert.Equal("Default-DNS", loadedOrder.Profile.Value);
            Assert.Null(loadedOrder.Error);

            Assert.Equal(638998550596190265, loadedOrder.Version);
        }
    }
    }

internal static class OrderJsonFileVariants
    {
    public const string Order_SV1_FullModel = """
        {
            "$id": "1",
            "SerializationVersion": 2,
            "OrderId": "pqyaNlRFVUakjv8Rr36I3Q",
            "AccountId": "Y47V6u5uoEu-GNAuWaQ16w",
            "Status": "Valid",
            "Identifiers": {
                "$id": "2",
                "$values": [
                    {
                        "$id": "3",
                        "SerializationVersion": 1,
                        "Type": "dns",
                        "Value": "site.web.th11s.corp",
                        "Metadata": {
                            "$id": "4"
                        }
                    },
                    {
                        "$id": "5",
                        "SerializationVersion": 1,
                        "Type": "dns",
                        "Value": "site-a.web.th11s.corp",
                        "Metadata": {
                            "$id": "6"
                        }
                    }
                ]
            },
            "Authorizations": {
                "$id": "7",
                "$values": [
                    {
                        "$id": "8",
                        "SerializationVersion": 1,
                        "AuthorizationId": "hyvU3onKkEy1SkI12oexPg",
                        "Status": "Valid",
                        "Identifier": {
                            "$ref": "3"
                        },
                        "IsWildcard": false,
                        "Expires": "2025-11-28T16:43:54.5204447+01:00",
                        "Challenges": {
                            "$id": "9",
                            "$values": [
                                {
                                    "$id": "10",
                                    "SerializationVersion": 1,
                                    "ChallengeId": "PeSCw0UVGEWDP3xwAR2hGQ",
                                    "Status": "Valid",
                                    "Type": "http-01",
                                    "Token": "-AdmkBybHNzwpGwqJUa56biHhkjtUXEF8XmZJVxsmQ5g9Ho3lfa_rqB1BR2zG35v",
                                    "Payload": "e30",
                                    "Validated": "2025-11-27T16:43:54.7569754+01:00",
                                    "Error": null
                                }
                            ]
                        }
                    },
                    {
                        "$id": "11",
                        "SerializationVersion": 1,
                        "AuthorizationId": "okoUHL0ae0Cxc53SxV9WOA",
                        "Status": "Valid",
                        "Identifier": {
                            "$ref": "5"
                        },
                        "IsWildcard": false,
                        "Expires": "2025-11-28T16:43:54.5204447+01:00",
                        "Challenges": {
                            "$id": "12",
                            "$values": [
                                {
                                    "$id": "13",
                                    "SerializationVersion": 1,
                                    "ChallengeId": "cXscQ-fPwEe8keZ6cx9byg",
                                    "Status": "Valid",
                                    "Type": "http-01",
                                    "Token": "mFXNL02t38jWvHD99F2lZd2rA_Oo0kXLQX49HixPV6kveAzbQPmzZwvyQOAfm7GN",
                                    "Payload": "e30",
                                    "Validated": "2025-11-27T16:43:59.7110854+01:00",
                                    "Error": null
                                }
                            ]
                        }
                    }
                ]
            },
            "NotBefore": null,
            "NotAfter": null,
            "Expires": "2027-11-27T16:34:11+01:00",
            "Profile": "Default-DNS",
            "Error": null,
            "Version": 638998550596190265,
            "CertificateSigningRequest": "MIIDqzCCAhMCAQAwHjEcMBoGA1UEAwwTc2l0ZS53ZWIudGgxMXMuY29ycDCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAIhl5YrTTonUAGaqx82pbFMpnggvSbGX444t16EnlToT3IV6P-RjQSgOYjnkC8xcTmcJs-ZTSYcDiI310im485ZzfUBmkBT2wXqXHn9g7n3E0GyCVx4LVf8lSbJZIEyzKvLINNpjKu4bAel6vAiDHnOkHM_ySnCLVnzt9Tc4hTxQvrLvs7bUO1M7Tpt5hrV29dCQRreuvPfa10CjVoWOYiz2ufb-KVDvoK4qIqq7M0zWdjs-hRnKdKX2MmI8L3NiNWVTUlTn4YuCE9003Fma1N-bRfpuUbi3-goXOtQx2QN9sVDCIfVQQzN7cL-N9MkzuMNvgEy3w-XoNFtz_-XzH0St8rqSu0SSnLaycYcgPPnj97QUkTR-Du5Oj-EzBU4KMY2XJgwvikzDlWtAQGl5z7sLIpSCgvCd4VLs9TmTbQ4yHMUn0lHd3Ek_JDyFViwsyNISmDSFFoH1w0ql7_jpK8RU0V8zleR-0aEOjpZ6pVt9r20ATtEygAx47fV2UJXGmQIDAQABoEgwRgYJKoZIhvcNAQkOMTkwNzA1BgNVHREELjAsghNzaXRlLndlYi50aDExcy5jb3JwghVzaXRlLWEud2ViLnRoMTFzLmNvcnAwDQYJKoZIhvcNAQENBQADggGBAEMR_LKFhWYHpzW4hjpv35ehSLZL-ii2M_1p5s5oiig59g0KtjiXmr3WSPRlMS072vxMjLDx3VWBmND7dmu7CWnxPPLM9Toi0kY4kw6uknJHQfzeF2e_mC0NGjJPO0zc1fX3Bphn3qERsr_GiOVI7poSNOpQuBeVJwR-Tbk9wxPW21Kxct-3jQn25ok11olWFKUqO3kjaSqm0AJOpXKKq5woFeDRMRZicNn9mje4Ci4PLDX-EdIaWJ2o5LdaacNEBj1ylVAMMiLIx3aYBkRLjuSUvNqSkibJBXUDANiWq4uprIr5L9VA2g09twXi4_kxhqtGZHzzJrC8Q4wokuLIKsR6DFOEAFGAtaeT_jf72Pqu8kA9riY82ZGbTeVJK2Z2ftgNesnf2VkwJuFKUpFmVdSedxDIlEeIXXIJojlAfUgxdFv18SXcdXjdsXMJ9V_nzcI1lyPnUgelpssvpNwor6MvBDJxkHdseoNxs_YnY5xrIU4p91URa44nB-m4gz2Q-Q",
            "CertificateId": "AA2uUdvkKt8WN56E0rTWXs1Yybo.OAAAAEQ9nH1TRsvuHQAAAAAARA"
        }
        """;
        Directory.CreateDirectory(Options.OrderDirectory);

        var orderId = new OrderId("pqyaNlRFVUakjv8Rr36I3Q");
        var orderFilePath = Path.Combine(Options.OrderDirectory, $"{orderId.Value}.json");
        await File.WriteAllTextAsync(orderFilePath, orderContent, TestContext.Current.CancellationToken);

        var sut = new OrderStore(new OptionsWrapper<FileStoreOptions>(Options), NullLogger<OrderStore>.Instance);
        var loadedOrder = await sut.LoadOrderAsync(orderId, TestContext.Current.CancellationToken);

        Assert.NotNull(loadedOrder);

        if (loadedOrder != null)
        {
            Assert.Equal(orderId, loadedOrder.OrderId);
            Assert.Equal("Y47V6u5uoEu-GNAuWaQ16w", loadedOrder.AccountId.Value);
            Assert.Equal(OrderStatus.Valid, loadedOrder.Status);
            Assert.Equal(DateTimeOffset.Parse("2027-11-27T16:34:11+01:00"), loadedOrder.Expires);

            Assert.Equal(2, loadedOrder.Identifiers.Count);
            Assert.Equal(2, loadedOrder.Authorizations.Count);
            Assert.Equal(loadedOrder.Authorizations.FirstOrDefault()?.Identifier, loadedOrder.Identifiers.FirstOrDefault());

            Assert.Null(loadedOrder.NotAfter);
            Assert.Null(loadedOrder.NotBefore);
            Assert.Equal("Default-DNS", loadedOrder.Profile.Value);
            Assert.Null(loadedOrder.Error);

            Assert.Equal(638998550596190265, loadedOrder.Version);
        }
    }
}