using ACMEServer.Storage.FileSystem.Configuration;
using ACMEServer.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;
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

        var sut = new AccountStore(new OptionsWrapper<FileStoreOptions>(Options));
        await sut.SaveAccountAsync(account, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, account.AccountId.Value, "account.json")));
        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, account.Jwk.KeyHash)));
    }

    [Fact]
    public async Task Saved_Accounts_Can_Be_Found_And_Loaded()
    {
        var jwk = JsonWebKeyFactory.CreateRsaJsonWebKey().ToAcmeJwk();
        var account = new Account(jwk, ["mailto:some@th11s.de"], DateTimeOffset.Now, null);

        var sut = new AccountStore(new OptionsWrapper<FileStoreOptions>(Options));
        await sut.SaveAccountAsync(account, CancellationToken.None);

        var foundAccount = await sut.FindAccountAsync(jwk, CancellationToken.None);
        Assert.NotNull(foundAccount);

        if(foundAccount != null)
        {
            Assert.Equal(account.AccountId, foundAccount.AccountId);
            Assert.Equal(account.Jwk.KeyHash, foundAccount.Jwk.KeyHash);
            Assert.Equal(account.Contacts, foundAccount.Contacts);
            Assert.Equal(account.TOSAccepted, foundAccount.TOSAccepted);
            Assert.Equal(account.Status, foundAccount.Status);
            Assert.Equal(account.Version, foundAccount.Version);
        }
    }
}

public class OrderStoreTests : StoreTestBase
{
    [Fact]
    public async Task Saving_an_Order_Creates_Order_File_and_Reference_File()
    {
        var order = new Order(new(), [new(IdentifierTypes.DNS, "example.th11s.de")]);

        var sut = new OrderStore(new OptionsWrapper<FileStoreOptions>(Options), NullLogger<OrderStore>.Instance);
        await sut.SaveOrderAsync(order, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Options.OrderDirectory, $"{order.OrderId.Value}.json")), "Order was not saved at expected path.");
        Assert.True(File.Exists(Path.Combine(Options.AccountDirectory, order.AccountId.Value, "orders", order.OrderId.Value)), "Reference File was not saved at expected path.");
    }

    [Fact]
    public async Task Saved_Orders_Can_Be_Loaded()
    {
        var order = new Order(new(), [new(IdentifierTypes.DNS, "example.th11s.de")]);
        order.Profile = new ("some-profile");

        var sut = new OrderStore(new OptionsWrapper<FileStoreOptions>(Options), NullLogger<OrderStore>.Instance);
        await sut.SaveOrderAsync(order, CancellationToken.None);

        var loadedOrder = await sut.LoadOrderAsync(order.OrderId, CancellationToken.None);
        Assert.NotNull(loadedOrder);

        if(loadedOrder != null)
        {
            Assert.Equal(order.OrderId, loadedOrder.OrderId);
            Assert.Equal(order.AccountId, loadedOrder.AccountId);
            Assert.Equal(order.Status, loadedOrder.Status);
            Assert.Equal(order.Expires, loadedOrder.Expires);

            Assert.Equal(order.Identifiers.Count, loadedOrder.Identifiers.Count);
            Assert.Equal(order.Authorizations.Count, loadedOrder.Authorizations.Count);

            Assert.Equal(order.NotAfter, loadedOrder.NotAfter);
            Assert.Equal(order.NotBefore, loadedOrder.NotBefore);
            Assert.Equal(order.Profile, loadedOrder.Profile);
            Assert.Equal(order.Error, loadedOrder.Error);

            Assert.Equal(order.Version, loadedOrder.Version);
        }
    }
}