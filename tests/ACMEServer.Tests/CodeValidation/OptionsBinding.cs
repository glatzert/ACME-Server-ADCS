using Microsoft.Extensions.Configuration;
using Th11s.ACMEServer.Configuration;

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
            item => Assert.Equal("example.org", item));
    }
}
