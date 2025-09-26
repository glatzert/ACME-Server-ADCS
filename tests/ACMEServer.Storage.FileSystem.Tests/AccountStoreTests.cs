using ACMEServer.Storage.FileSystem.Configuration;
using ACMEServer.Tests.Utils;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;

namespace ACMEServer.Storage.FileSystem.Tests;

public class AccountStoreTests : StoreTestBase
{
    [Fact]
    public async Task Saving_an_Account_Creates_Account_File_And_JWK_Index_File()
    {
        var jwk = JsonWebKeyFactory.CreateRsaJsonWebKey().ToAcmeJwk();

        var account = new Account(jwk, ["mailto:some@th11s.de"], DateTimeOffset.Now, null);

        var accountStore = new AccountStore(new OptionsWrapper<FileStoreOptions>(Options));
        await accountStore.SaveAccountAsync(account, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, account.AccountId.Value, "account.json")));
        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, account.Jwk.KeyHash)));
    }

    [Fact]
    public async Task Saved_Accounts_Can_Be_Found_And_Loaded()
    {
        var jwk = JsonWebKeyFactory.CreateRsaJsonWebKey().ToAcmeJwk();
        var account = new Account(jwk, ["mailto:some@th11s.de"], DateTimeOffset.Now, null);

        var accountStore = new AccountStore(new OptionsWrapper<FileStoreOptions>(Options));
        await accountStore.SaveAccountAsync(account, CancellationToken.None);

        var foundAccount = await accountStore.FindAccountAsync(jwk, CancellationToken.None);
        Assert.NotNull(foundAccount);

        if(foundAccount != null)
        {
            Assert.Equal(account.AccountId.Value, foundAccount.AccountId.Value);
            Assert.Equal(account.Jwk.KeyHash, foundAccount.Jwk.KeyHash);
            Assert.Equal(account.Contacts, foundAccount.Contacts);
            Assert.Equal(account.TOSAccepted, foundAccount.TOSAccepted);
            Assert.Equal(account.Status, foundAccount.Status);
            Assert.Equal(account.Version, foundAccount.Version);
        }
    }
}
