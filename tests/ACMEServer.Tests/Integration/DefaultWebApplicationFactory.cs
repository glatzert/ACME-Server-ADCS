using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Tests.Utils.Fakes;

namespace Th11s.ACMEServer.Tests.Integration;

public class DefaultWebApplicationFactory
    : WebApplicationFactory<Program>, IDisposable
{
    internal string StoragePath { get; set; }
    internal Dictionary<string, string?> AdditionalConfigSettings { get; }

    public DefaultWebApplicationFactory()
        : this([])
    {
        
    }

    internal DefaultWebApplicationFactory(Dictionary<string, string?> additionalConfigSettings)
    {
        StoragePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(StoragePath);
        AdditionalConfigSettings = additionalConfigSettings;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            var webConfig = new Dictionary<string, string?>()
            {
                { "AcmeFileStore:BasePath", StoragePath }
            };

            config.AddInMemoryCollection(webConfig);
            config.AddInMemoryCollection(AdditionalConfigSettings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IChallengeValidator>();
            services.AddScoped<IChallengeValidator>((_) => new FakeChallengeValidator(ChallengeTypes.Http01, [IdentifierTypes.DNS, IdentifierTypes.IP]));
            services.AddScoped<IChallengeValidator>((_) => new FakeChallengeValidator(ChallengeTypes.Dns01, [IdentifierTypes.DNS]));
            services.AddScoped<IChallengeValidator>((_) => new FakeChallengeValidator(ChallengeTypes.TlsAlpn01, [IdentifierTypes.DNS, IdentifierTypes.IP]));

            services.Configure<ACMEServerOptions>(config =>
            {
                config.HostedWorkers.ValidationCheckInterval = 1;
                config.HostedWorkers.IssuanceCheckInterval = 1;
                config.TOS.RequireAgreement = true;
            });

            services.AddScoped<ICertificateIssuer>((_) => new FakeCertificateIssuer());
            services.AddScoped<ICAAEvaluator>((_) => new FakeCAAEvaluator());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (Directory.Exists(StoragePath))
        {
            Directory.Delete(StoragePath, true);
        }
    }
}