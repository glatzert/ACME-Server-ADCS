using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;

namespace Th11s.AcmeServer.Tests.Integration;

public class DirectoryRetrievalTests
{
    private async Task<(Certify.ACME.Anvil.Acme.Resource.Directory directory, Uri baseUrl)> RetrieveDirectoryWithConfig(string relativeUri, Dictionary<string, string?> config)
    {
        using var _factory = new DefaultWebApplicationFactory(config);

        var baseUrl = _factory.Server.BaseAddress;

        var context = new AcmeContext(new Uri(baseUrl, relativeUri), http: new AcmeHttpClient(baseUrl, _factory.CreateClient()));
        var directory = await context.GetDirectory(true);

        return (directory, baseUrl);
    }

    [Theory,
        InlineData("/"),
        InlineData("/directory")]
    public async Task Directory_Retrieval_Works(string relativeUri)
    {
        var (directory, baseUrl) = await RetrieveDirectoryWithConfig(relativeUri, new Dictionary<string, string?>());

        Assert.NotNull(directory);
        Assert.Equal(new Uri(baseUrl, "/new-nonce"), directory.NewNonce);
        Assert.Equal(new Uri(baseUrl, "/new-account"), directory.NewAccount);
        Assert.Equal(new Uri(baseUrl, "/new-order"), directory.NewOrder);
        Assert.Equal(new Uri(baseUrl, "/key-change"), directory.KeyChange);
        Assert.Equal(new Uri(baseUrl, "/revoke-cert"), directory.RevokeCert);
        Assert.Null(directory.RenewalInfo);
        
        Assert.NotNull(directory.Meta);
        Assert.NotEmpty(directory.Meta.Profiles);
        Assert.NotEmpty(directory.Meta.CaaIdentities);
        Assert.False(directory.Meta.ExternalAccountRequired);
        Assert.Null(directory.Meta.TermsOfService);
    }

    [Fact]
    public async Task TOS_is_reflected_in_Directory()
    {
        var config = new Dictionary<string, string?>()
        {
            { "ACMEServer:TOS:RequireAgreement", "false" },
            { "ACMEServer:TOS:URL", "https://example.com/tos" },
        };

        var (directory, _) = await RetrieveDirectoryWithConfig("/", config);

        Assert.Equal("https://example.com/tos", directory.Meta.TermsOfService?.ToString());
    }

    [Fact]
    public async Task EAB_is_reflected_in_Directory()
    {
        var config = new Dictionary<string, string?>()
        {
            { "ACMEServer:ExternalAccountBinding:Required", "true" }
        };

        var (directory, _) = await RetrieveDirectoryWithConfig("/", config);

        Assert.True(directory.Meta.ExternalAccountRequired);
    }

    //public async Task Directory_Data_Reflects_Config()
    //{
    //    _factory.Server.Services.Configure<ACMEServerOptions>(config =>
    //    {
    //        config.TOS.RequireAgreement = false;
    //        config.TOS.URL = new Uri("https://example.com/tos");
    //        config.EAB.Required = true;
    //    });
    //}
    // TODO: Add tests for directory retrieval with different configurations (e.g. TOS and EAB)
}