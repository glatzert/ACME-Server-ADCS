using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;
using Microsoft.IdentityModel.Tokens;

namespace Th11s.ACMEServer.Tests.Integration.ExternalAccountBinding;

public class ExternalBoundAccountManagementTests
    : IClassFixture<ExternalAccountBindingWebApplicationFactory>
{
    private readonly ExternalAccountBindingWebApplicationFactory _factory;

    public ExternalBoundAccountManagementTests(ExternalAccountBindingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<AcmeContext> CreateAcmeContext()
    {
        var httpClient = _factory.CreateClient();
        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        return acme;
    }


    [Fact]
    public async Task Create_Account_With_External_Account_Binding()
    {
        var acme = await CreateAcmeContext();
        var account = await acme.NewAccount("test@example.com", true, "keyId", Base64UrlEncoder.Encode(ExternalAccountBindingWebApplicationFactory.EABKey), "HS256");

        var accountResource = await account.Resource();
        Assert.NotNull(accountResource.ExternalAccountBinding);
    }

    

    [Fact]
    public async Task Create_Account_With_Invalid_External_Account_Binding()
    {
        var acme = await CreateAcmeContext();

        var ex = await Assert.ThrowsAsync<AcmeRequestException>(
            () => acme.NewAccount(
                "test@example.com", 
                true, 
                "invalid", 
                Base64UrlEncoder.Encode(ExternalAccountBindingWebApplicationFactory.EABKey), 
                "HS256"));
        
        Assert.Contains("Test not okay", ex.Message);
    }
}