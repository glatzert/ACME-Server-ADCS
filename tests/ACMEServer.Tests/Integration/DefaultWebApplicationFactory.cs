using Th11s.ACMEServer.ADCS;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Model.Storage;
using ACMEServer.Storage.InMemory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Configuration;

namespace ACMEServer.Tests.Integration;

public class DefaultWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<INonceStore, InMemoryNonceStore>();
            services.AddSingleton<IAccountStore, InMemoryAccountStore>();
            services.AddSingleton<IOrderStore, InMemoryOrderStore>();

            services.RemoveAll<IChallengeValidator>();
            services.AddScoped<IChallengeValidator>((_) => new FakeChallengeValidator(ChallengeTypes.Http01));
            services.AddScoped<IChallengeValidator>((_) => new FakeChallengeValidator(ChallengeTypes.Dns01));
            services.AddScoped<IChallengeValidator>((_) => new FakeChallengeValidator(ChallengeTypes.TlsAlpn01));

            services.Configure<ACMEServerOptions>(config =>
            {
                config.HostedWorkers.ValidationCheckInterval = 1;
                config.HostedWorkers.IssuanceCheckInterval = 1;
            });

            services.AddScoped<ICertificateIssuer>((_) => new FakeCertificateIssuer());
        });

        builder.UseEnvironment("Development");
    }
}