using Microsoft.Extensions.Configuration;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Tests.CodeValidation;

public class OptionsBinding
{
    /// <summary>
    /// This test exists, that we can bind a Collection from the configuration correctly.
    /// </summary>
    [Fact]
    public void CAAIdentities_Bind_Correctly()
    {
        var options = new ACMEServerOptions();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CAAIdentities:0"] = "example.com",
                ["CAAIdentities:1"] = "example.org"
            })
            .Build();

        configuration.Bind(options);

        Assert.NotNull(options.CAAIdentities);
        Assert.Collection(options.CAAIdentities,
            item => Assert.Equal("example.com", item),
            item => Assert.Equal("example.org", item)
        );
    }

    [Fact]
    public void IPAddressValidation_Binds_Correctly()
    {
        var options = new ProfileConfiguration();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IdentifierValidation:IP:AllowedIPNetworks:0"] = "1.2.3.4/32"
            })
            .Build();

        configuration.Bind(options);

        Assert.NotNull(options.IdentifierValidation.IP.AllowedIPNetworks);
        Assert.Collection(options.IdentifierValidation.IP.AllowedIPNetworks,
            item => Assert.Equal("1.2.3.4/32", item)
        );
    }
}
