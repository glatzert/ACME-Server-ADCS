using ACMEServer.ADCS.IntegrationTests.EAB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Services;

namespace ACMEServer.ADCS.IntegrationTests;

public class EABACMEADCSWebApplicationFactory
    : ACMEADCSWebApplicationFactory
{
    public const string EABKey = "BHDblFHAzVmcqfPPrdBUYXAqNlxfpdAg";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            ctx.Configuration["ACMEServer:ExternalAccountBinding:Required"] = "true";
        });

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IExternalAccountBindingClient, FakeExternalAccountBindingClient>();
        });

        builder.UseEnvironment("Development");
    }
}