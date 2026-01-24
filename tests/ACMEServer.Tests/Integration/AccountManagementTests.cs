using Certify.ACME.Anvil;
using Certify.ACME.Anvil.Acme;
using Certify.ACME.Anvil.Acme.Resource;

namespace Th11s.ACMEServer.Tests.Integration;

public class AccountManagementTests
    : IClassFixture<DefaultWebApplicationFactory>
{
    private readonly DefaultWebApplicationFactory _factory;

    private async Task<AcmeContext> CreateAcmeContextAsync(IKey? accountKey = null)
    {
        var result = accountKey is null
            ? new AcmeContext(
                _factory.Server.BaseAddress, 
                http: new AcmeHttpClient(_factory.Server.BaseAddress, _factory.CreateClient())
            )
            : new AcmeContext(
                _factory.Server.BaseAddress,
                accountKey,
                http: new AcmeHttpClient(_factory.Server.BaseAddress, _factory.CreateClient())
            );

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
        Assert.Equal(AccountStatus.Valid, initialAccount.Status);
        Assert.True(initialAccount.TermsOfServiceAgreed);
        Assert.Equal("mailto:test@example.com", initialAccount.Contact[0]);


        var changedAccount = await account.Update(contact: ["mailto:test2@example.com"], agreeTermsOfService: true);
        Assert.Equal("mailto:test2@example.com", changedAccount.Contact[0]);

        var disabledAccount = await account.Deactivate();
        Assert.Equal(AccountStatus.Deactivated, disabledAccount.Status);

        var updateException = await Assert.ThrowsAsync<AcmeRequestException>(() => account.Update(agreeTermsOfService: true));
        Assert.Contains("urn:ietf:params:acme:error:unauthorized", updateException?.Message);
    }


    [Fact]
    public async Task Create_Account_ChangeKey()
    {
        var acme = await CreateAcmeContextAsync();

        var account = await acme.NewAccount("test@example.com", true);
        var initialAccount = await account.Resource();
        Assert.Equal(AccountStatus.Valid, initialAccount.Status);
        Assert.True(initialAccount.TermsOfServiceAgreed);
        Assert.Equal("mailto:test@example.com", initialAccount.Contact[0]);

        var newKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
        var rekeyedAccount = await acme.ChangeKey(newKey);

        acme = await CreateAcmeContextAsync(newKey);
        var reloadedAccount = await acme.Account();

        Assert.Equal(account.Location, reloadedAccount.Location);
    }


    [Fact]
    public async Task Unknwon_AccountKey_Throws_Acme_Exception()
    {
        var acme = await CreateAcmeContextAsync();

        var ex = await Assert.ThrowsAsync<AcmeRequestException>(() => acme.Account());
        Assert.Contains("urn:ietf:params:acme:error:accountDoesNotExist", ex.Message);
    }


    [Fact]
    public async Task Missing_TOSAgreement_Throws_Acme_Exception()
    {
        var acme = await CreateAcmeContextAsync();

        var newAccountException = await Assert.ThrowsAsync<AcmeRequestException>(() => acme.NewAccount("test@example.com", false));
        Assert.Contains("urn:ietf:params:acme:error:userActionRequired", newAccountException?.Message);
    }
}
