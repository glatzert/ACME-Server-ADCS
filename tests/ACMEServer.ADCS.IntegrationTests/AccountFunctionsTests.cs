using Certify.ACME.Anvil.Acme;
using Certify.ACME.Anvil;
using Th11s.ACMEServer.ADCS;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Model.Storage;
using ACMEServer.Storage.InMemory;

namespace ACMEServer.ADCS.IntegrationTests;

public class AccountFunctionsTests
    : IClassFixture<ACMEADCSWebApplicationFactory>
{
    private readonly ACMEADCSWebApplicationFactory _factory;

    public AccountFunctionsTests(ACMEADCSWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Can_Create_Change_and_deactivate_account()
    {
        var httpClient = _factory.CreateClient();

        var acme = new AcmeContext(_factory.Server.BaseAddress, http: new AcmeHttpClient(_factory.Server.BaseAddress, httpClient));
        await acme.GetDirectory(true);
        var account = await acme.NewAccount("test@example.com", true);

        // Save the account key for later use
        var pemKey = acme.AccountKey.ToPem();

        await account.Update(contact: ["test2@example.com"], agreeTermsOfService: true);
        await account.Deactivate();

        await Assert.ThrowsAnyAsync<Exception>(() => account.Update(agreeTermsOfService: true));
    }
}

public class ACMEADCSWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<INonceStore, InMemoryNonceStore>();
            services.AddSingleton<IAccountStore, InMemoryAccountStore>();
            services.AddSingleton<IOrderStore, InMemoryOrderStore>();
        });

        builder.UseEnvironment("Development");
    }
}