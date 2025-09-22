using Certify.ACME.Anvil.Acme;
using Certify.ACME.Anvil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;

namespace Th11s.AcmeServer.Tests.Integration;

public class DirectoryRetrievalTests : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    public DirectoryRetrievalTests(DefaultWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory,
        InlineData("/"),
        InlineData("/directory")]
    public async Task Directory_Retrieval_Works(string relativeUri)
    {
        var baseUrl = _factory.Server.BaseAddress;

        var context = new AcmeContext(new Uri(baseUrl, relativeUri), http: new AcmeHttpClient(baseUrl, _factory.CreateClient()));
        var directory = await context.GetDirectory(true);

        Assert.NotNull(directory);
        Assert.Equal(new Uri(baseUrl, "/new-nonce"), directory.NewNonce);
        Assert.Equal(new Uri(baseUrl, "/new-account"), directory.NewAccount);
        Assert.Equal(new Uri(baseUrl, "/new-order"), directory.NewOrder);
        Assert.Null(directory.RenewalInfo);
        Assert.Null(directory.RevokeCert);
        Assert.Null(directory.KeyChange);
        Assert.NotNull(directory.Meta);
        Assert.NotEmpty(directory.Meta.Profiles);
        Assert.False(directory.Meta.ExternalAccountRequired);
        Assert.Null(directory.Meta.TermsOfService);
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