using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        IDictionary<string, ProfileConfiguration> _profileDescriptors = new Dictionary<string, ProfileConfiguration>()
        {
            ["dns-or-ip"] = new ProfileConfiguration
            {
                Name = "dns-or-ip",
                SupportedIdentifiers = new[] { "dns", "ip" },
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                }
            },
            ["dns"] = new ProfileConfiguration
            {
                Name = "dns",
                SupportedIdentifiers = new[] { "dns" },
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                }
            },
            ["ip"] = new ProfileConfiguration
            {
                Name = "ip",
                SupportedIdentifiers = new[] { "ip" },
                ADCSOptions = new ADCSOptions
                {
                    CAServer = "http://localhost",
                    TemplateName = "WebServer"
                }
            },
            ["device"] = new ProfileConfiguration
            {
                Name = "device",
                SupportedIdentifiers = new[] { "permanent-identifier" },
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
            var order = new Order("accountId", identifierTypes
                 .Select(type => new Identifier(type, "test")));

            var sut = new DefaultIssuanceProfileSelector(
                Options.Create(_profiles),
                new FakeOptionSnapshot<ProfileConfiguration>(_profileDescriptors)
            );

            var profile = await sut.SelectProfile(order, false, ProfileName.None, default);
                
            Assert.Equal(new ProfileName(expecedProfile), profile);
        }
    }
}
