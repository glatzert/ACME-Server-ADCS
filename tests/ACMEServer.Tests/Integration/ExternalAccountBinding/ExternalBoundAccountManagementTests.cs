using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;
using Microsoft.IdentityModel.Tokens;

namespace ACMEServer.Tests.Integration.ExternalAccountBinding;

public class ExternalBoundAccountManagementTests
    : IClassFixture<ExternalAccountBindingWebApplicationFactory>
{
    private readonly ExternalAccountBindingWebApplicationFactory _factory;

    public ExternalBoundAccountManagementTests(ExternalAccountBindingWebApplicationFactory factory)
    {
        _factory = factory;
    }


    [Fact]
    public async Task Create_Account_With_External_Account_Binding()
    {
        var httpClient = _factory.CreateClient();
        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        var account = await acme.NewAccount("test@example.com", true, "keyId", Base64UrlEncoder.Encode(ExternalAccountBindingWebApplicationFactory.EABKey), "HS256");

        var accountResource = await account.Resource();
        Assert.NotNull(accountResource.ExternalAccountBinding);
    }

    [Fact]
    public async Task Create_Account_With_Invalid_External_Account_Binding()
    {
        var httpClient = _factory.CreateClient();
        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        try
        {
            var account = await acme.NewAccount("test@example.com", true, "invalid", Base64UrlEncoder.Encode(ExternalAccountBindingWebApplicationFactory.EABKey), "HS256");
        }
        catch (AcmeRequestException ex)
        {
            Assert.Contains("Test not okay", ex.Message);
        }
    }
}