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
        Dictionary<ProfileName, ProfileConfiguration> _profileDescriptors = new Dictionary<ProfileName, ProfileConfiguration>()
        {
            [new("dns-or-ip")] = new ProfileConfiguration
            {
                Name = "dns-or-ip",
                SupportedIdentifiers = ["dns", "ip"],
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                },
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
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                },
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
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                },
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
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                }
            }
        };

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
            var fakeProfileProvider = new FakeProfileProvider(_profileDescriptors);

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
