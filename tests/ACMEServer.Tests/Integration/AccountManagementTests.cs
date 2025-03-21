using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;
using Certify.ACME.Anvil.Acme.Resource;

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
        var initialAccount = await account.Resource();
        var changedAccount = await account.Update(contact: ["mailto:test2@example.com"], agreeTermsOfService: true);
        var disabledAccount = await account.Deactivate();

        AcmeRequestException? updateException = null;
        try
        {
            _ = await account.Update(agreeTermsOfService: true);
        }
        catch (AcmeRequestException ex)
        {
            updateException = ex;
        }


        Assert.Equal(AccountStatus.Valid, initialAccount.Status);
        Assert.True(initialAccount.TermsOfServiceAgreed);
        
        Assert.Equal("mailto:test@example.com", initialAccount.Contact[0]);

        Assert.Equal("mailto:test2@example.com", changedAccount.Contact[0]);

        Assert.Equal(AccountStatus.Deactivated, disabledAccount.Status);

        Assert.NotNull(updateException);
        Assert.Contains("urn:ietf:params:acme:error:malformed", updateException?.Message);
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
            Assert.Contains("urn:ietf:params:acme:error:accountDoesNotExist", ex.Message);
        }
    }

    [Fact]
    public async Task Missing_TOSAgreement_Throws_Acme_Exception()
    {
        var acme = await CreateAcmeContextAsync();

        AcmeRequestException? newAccountException = null;
        try {
            var account = await acme.NewAccount("test@example.com", false);
        }
        catch (AcmeRequestException ex)
        {
            newAccountException = ex;
        }

        Assert.NotNull(newAccountException);
        Assert.Contains("urn:ietf:params:acme:error:userActionRequired", newAccountException?.Message);
    }
}