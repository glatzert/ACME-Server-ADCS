using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;

namespace ACMEServer.Tests.Integration;

public class AuthorizationCreationTests : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    public AuthorizationCreationTests(DefaultWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Order_And_Get_Certificate()
    {
        var httpClient = _factory.CreateClient();

        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        var account = await acme.NewAccount("test@example.com", true);
        acme.SetAccountUri(account.Location);

        var order = await acme.NewOrder(["*.example.com"]);
        var authorizations = await order.Authorizations();

        var authorization = await authorizations.First().Resource();

        Assert.Single(authorizations);
        Assert.Single(authorization.Challenges);

        Assert.True(authorization.Wildcard);
        Assert.Equal("example.com", authorization.Identifier.Value);
    }
}