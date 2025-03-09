using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;
using Certify.ACME.Anvil.Acme.Resource;
using System.Net.Http;

namespace ACMEServer.Tests.Integration;

public class AccountManagementTests
    : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    private async Task<AcmeContext> CreateAcmeContextAsync()
    {
        var result = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, _factory.CreateClient()));
        await result.GetDirectory(true);
        return result;
    }

    public AccountManagementTests(DefaultWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Account_Update_Account_And_Deactivate()
    {
        var acme = await CreateAcmeContextAsync();
        

        var account = await acme.NewAccount("test@example.com", true);

        var accountResource = await account.Resource();
        Assert.Equal(AccountStatus.Valid, accountResource.Status);
        Assert.Equal("test@example.com", accountResource.Contact[0]);

        await account.Update(contact: ["test2@example.com"], agreeTermsOfService: true);
        
        accountResource = await account.Resource();
        Assert.Equal("test2@example.com", accountResource.Contact[0]);

        await account.Deactivate();
        accountResource = await account.Resource();

        Assert.Equal(AccountStatus.Deactivated, accountResource.Status);

        try
        {
            var updatedAccount = account.Update(agreeTermsOfService: true);
        }
        catch (AcmeRequestException ex)
        {
            Assert.Contains(ex.Message, "urn:ietf:params:acme:error:accountDoesNotExist");
        }
    }

    [Fact]
    public async Task Unknwon_AccountKey_Throws_Acme_Exception()
    {
        var acme = await CreateAcmeContextAsync();

        try
        {
            var account =  await acme.Account();
        }
        catch (AcmeRequestException ex)
        {
            Assert.Contains(ex.Message, "urn:ietf:params:acme:error:accountDoesNotExist");
        }
    }

    [Fact]
    public async Task Missing_TOSAgreement_Throws_Acme_Exception()
    {
        var acme = await CreateAcmeContextAsync();

        try {
            var account = await acme.NewAccount("test@example.com", false);
        }
        catch (AcmeRequestException ex)
        {
            Assert.Contains(ex.Message, "urn:ietf:params:acme:error:userActionRequired");
        }
    }
}