using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;
using Microsoft.IdentityModel.Tokens;

namespace ACMEServer.ADCS.IntegrationTests;

public class ExternalBoundAccountManagementTests
    : IClassFixture<EABACMEADCSWebApplicationFactory>
{
    private readonly EABACMEADCSWebApplicationFactory _factory;

    public ExternalBoundAccountManagementTests(EABACMEADCSWebApplicationFactory factory)
    {
        _factory = factory;
    }


    [Fact]
    public async Task Create_Account_With_External_Account_Binding()
    {
        var httpClient = _factory.CreateClient();
        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        var account = await acme.NewAccount("test@example.com", true, "keyId", Base64UrlEncoder.Encode(EABACMEADCSWebApplicationFactory.EABKey), "HS256");

        var accountResource = await account.Resource();
        Assert.NotNull(accountResource.ExternalAccountBinding);
    }
}