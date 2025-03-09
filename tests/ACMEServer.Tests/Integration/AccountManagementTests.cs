using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;

namespace ACMEServer.Tests.Integration;

public class AccountManagementTests
    : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    public AccountManagementTests(DefaultWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Account_Update_Account_And_Deactivate()
    {
        var httpClient = _factory.CreateClient();

        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        var account = await acme.NewAccount("test@example.com", true);

        await account.Update(contact: ["test2@example.com"], agreeTermsOfService: true);
        await account.Deactivate();

        await Assert.ThrowsAnyAsync<Exception>(() => account.Update(agreeTermsOfService: true));
    }
}