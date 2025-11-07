using ACMEServer.Tests.Utils.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.AcmeServer.Tests.Services
{
    public class DefaultIssuanceProfileSelectorTest
    {
        HashSet<ProfileName> _profiles = [
            new ProfileName("dns"),
            new ProfileName("ip"),
            new ProfileName("dns-or-ip"),
            new ProfileName("device"),
        ];

        Dictionary<string, ProfileConfiguration> _profileDescriptors = new Dictionary<string, ProfileConfiguration>()
        {
            ["dns-or-ip"] = new ProfileConfiguration
            {
                Name = "dns-or-ip",
                SupportedIdentifiers = ["dns", "ip"],
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                }
            },
            ["dns"] = new ProfileConfiguration
            {
                Name = "dns",
                SupportedIdentifiers = ["dns"],
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                }
            },
            ["ip"] = new ProfileConfiguration
            {
                Name = "ip",
                SupportedIdentifiers = ["ip"],
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                }
            },
            ["device"] = new ProfileConfiguration
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
            var optionsSnapshot = new FakeOptionSnapshot<ProfileConfiguration>(_profileDescriptors);

            var sut = new DefaultIssuanceProfileSelector(
                new DefaultIdentifierValidator(
                    new FakeCAAEvaluator(),
                    optionsSnapshot, 
                    NullLogger<DefaultIdentifierValidator>.Instance
                ),
                Options.Create(_profiles),
                optionsSnapshot,
                NullLogger<DefaultIssuanceProfileSelector>.Instance
            );

            var profile = await sut.SelectProfile(order, false, ProfileName.None, default);
                
            Assert.Equal(new ProfileName(expecedProfile), profile);
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
