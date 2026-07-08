using Microsoft.Extensions.Logging.Abstractions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Tests.Utils.Fakes;


namespace Th11s.ACMEServer.Tests.Services
{
    public class DefaultIssuanceProfileSelectorTest
    {
        static ADCSOptions _adcsOptions = new() { CAServer = "http://localhost", TemplateName = "WebServer" };

        [Fact]
        public async Task Profile_With_Higher_Priority_Will_Be_Preferred()
        {
            var order = new Order(new AccountId("accountId"), [new Identifier(IdentifierTypes.DNS, "example.com")]);
            var fakeProfileProvider = new FakeProfileProvider(new Dictionary<ProfileName, ProfileConfiguration>()
            {
                [new("dns-5")] = new ProfileConfiguration
                {
                    Name = "dns-5",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    Priority = 5,
                    IdentifierValidation = new IdentifierValidationParameters() {  DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
                [new("dns-1")] = new ProfileConfiguration
                {
                    Name = "dns-1",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    Priority = 1,
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
                [new("dns-10")] = new ProfileConfiguration
                {
                    Name = "dns-10",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    Priority = 10,
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
            });

            var sut = new DefaultIssuanceProfileSelector(
                new DefaultIdentifierValidator(
                    NullLogger<DefaultIdentifierValidator>.Instance
                ),
                fakeProfileProvider,
                NullLogger<DefaultIssuanceProfileSelector>.Instance
            );

            var profile = await sut.SelectProfile(
                new(
                    order,
                    new(new("accountId"), false),
                    ProfileName.None
                ),
                TestContext.Current.CancellationToken);

            Assert.Equal(new ProfileName("dns-10"), profile.ProfileName);
        }


        [Fact]
        public async Task Profile_With_EAB_Will_Be_Preferred()
        {
            var order = new Order(new AccountId("accountId"), [new Identifier(IdentifierTypes.DNS, "example.com")]);
            var fakeProfileProvider = new FakeProfileProvider(new Dictionary<ProfileName, ProfileConfiguration>()
            {
                [new("dns")] = new ProfileConfiguration
                {
                    Name = "dns",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    RequireExternalAccountBinding = false,
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
                [new("dns-eab")] = new ProfileConfiguration
                {
                    Name = "dns-eab",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    RequireExternalAccountBinding = true,
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
            });

            var sut = new DefaultIssuanceProfileSelector(
                new DefaultIdentifierValidator(
                    NullLogger<DefaultIdentifierValidator>.Instance
                ),
                fakeProfileProvider,
                NullLogger<DefaultIssuanceProfileSelector>.Instance
            );

            var profile = await sut.SelectProfile(
                new(
                    order,
                    new(new("accountId"), true),
                    ProfileName.None
                ),
                TestContext.Current.CancellationToken);

            Assert.Equal(new ProfileName("dns-eab"), profile.ProfileName);
        }


        [Fact]
        public async Task Profile_Alphabetically_Ordered_Profile()
        {
            var order = new Order(new AccountId("accountId"), [new Identifier(IdentifierTypes.DNS, "example.com")]);
            var fakeProfileProvider = new FakeProfileProvider(new Dictionary<ProfileName, ProfileConfiguration>()
            {
                [new("dns-z")] = new ProfileConfiguration
                {
                    Name = "dns-z",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
                [new("dns-a")] = new ProfileConfiguration
                {
                    Name = "dns-a",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
            });

            var sut = new DefaultIssuanceProfileSelector(
                new DefaultIdentifierValidator(
                    NullLogger<DefaultIdentifierValidator>.Instance
                ),
                fakeProfileProvider,
                NullLogger<DefaultIssuanceProfileSelector>.Instance
            );

            var profile = await sut.SelectProfile(
                new(
                    order,
                    new(new("accountId"), true),
                    ProfileName.None
                ),
                TestContext.Current.CancellationToken);

            Assert.Equal(new ProfileName("dns-a"), profile.ProfileName);
        }


        [Fact]
        public async Task Profile_With_less_Identifiers_Will_Be_Preferred()
        {
            var order = new Order(new AccountId("accountId"), [new Identifier(IdentifierTypes.DNS, "example.com")]);
            var fakeProfileProvider = new FakeProfileProvider(new Dictionary<ProfileName, ProfileConfiguration>()
            {
                [new("dns-or-ip")] = new ProfileConfiguration
                {
                    Name = "dns-or-ip",
                    SupportedIdentifiers = ["dns", "ip"],
                    CertificateServices = [_adcsOptions],
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
                [new("dns")] = new ProfileConfiguration
                {
                    Name = "dns",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    IdentifierValidation = new IdentifierValidationParameters() { DNS = new() { AllowedDNSNames = ["example.com"] } }
                },
            });

            var sut = new DefaultIssuanceProfileSelector(
                new DefaultIdentifierValidator(
                    NullLogger<DefaultIdentifierValidator>.Instance
                ),
                fakeProfileProvider,
                NullLogger<DefaultIssuanceProfileSelector>.Instance
            );

            var profile = await sut.SelectProfile(
                new(
                    order,
                    new(new("accountId"), true),
                    ProfileName.None
                ),
                TestContext.Current.CancellationToken);

            Assert.Equal(new ProfileName("dns"), profile.ProfileName);
        }


        [Theory,
            InlineData(["dns", "dns"]),
            InlineData(["ip", "ip"]),
            InlineData(["dns-or-ip", "dns", "ip"]),
            InlineData(["device", "permanent-identifier"]),
            ]
        public async Task ValidProfile_Will_Return_Profile(string expecedProfile, params string[] identifierTypes)
        {
            var order = new Order(
                new("accountId"), 
                identifierTypes.Select(CreateTestIdentifier)
                );

            var fakeProfileProvider = new FakeProfileProvider(new Dictionary<ProfileName, ProfileConfiguration>()
            {
                [new("dns-or-ip")] = new ProfileConfiguration
                {
                    Name = "dns-or-ip",
                    SupportedIdentifiers = ["dns", "ip"],
                    CertificateServices = [_adcsOptions],
                    IdentifierValidation = new IdentifierValidationParameters
                    {
                        DNS = new()
                        {
                            AllowedDNSNames = ["example.com"]
                        },
                        IP = new()
                        {
                            AllowedIPNetworks = ["::0/0", "0.0.0.0/0"]
                        }
                    }
                },
                [new("dns")] = new ProfileConfiguration
                {
                    Name = "dns",
                    SupportedIdentifiers = ["dns"],
                    CertificateServices = [_adcsOptions],
                    IdentifierValidation = new IdentifierValidationParameters
                    {
                        DNS = new()
                        {
                            AllowedDNSNames = ["example.com"]
                        }
                    }
                },
                [new("ip")] = new ProfileConfiguration
                {
                    Name = "ip",
                    SupportedIdentifiers = ["ip"],
                    CertificateServices = [_adcsOptions],
                    IdentifierValidation = new IdentifierValidationParameters
                    {
                        IP = new()
                        {
                            AllowedIPNetworks = ["::0/0", "0.0.0.0/0"]
                        }
                    }
                },
                [new("device")] = new ProfileConfiguration
                {
                    Name = "device",
                    SupportedIdentifiers = ["permanent-identifier"],
                    CertificateServices = [_adcsOptions]
                }
            });

            var sut = new DefaultIssuanceProfileSelector(
                new DefaultIdentifierValidator(
                    NullLogger<DefaultIdentifierValidator>.Instance
                ),
                fakeProfileProvider,
                NullLogger<DefaultIssuanceProfileSelector>.Instance
            );

            var profile = await sut.SelectProfile(
                new(
                    order, 
                    new(new("accountId"), false), 
                    ProfileName.None
                ), 
                TestContext.Current.CancellationToken);
                
            Assert.Equal(new ProfileName(expecedProfile), profile.ProfileName);
        }

        private Identifier CreateTestIdentifier(string type)
        {
            return type switch
            {
                IdentifierTypes.DNS => new Identifier(IdentifierTypes.DNS, "example.com"),
                IdentifierTypes.IP => new Identifier(IdentifierTypes.IP, "127.0.0.1"),
                IdentifierTypes.PermanentIdentifier => new Identifier(IdentifierTypes.PermanentIdentifier, "test"),
                IdentifierTypes.HardwareModule => new Identifier(IdentifierTypes.HardwareModule, "test"),
                IdentifierTypes.Email => new Identifier(IdentifierTypes.Email, "test@example.com"),

                _ => throw new ArgumentException($"Unknown identifier type: {type}")
            };
        }
    }
}
