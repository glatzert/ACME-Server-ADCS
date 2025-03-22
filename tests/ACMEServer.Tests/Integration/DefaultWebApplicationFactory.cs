﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace ACMEServer.Tests.Integration;

public class DefaultWebApplicationFactory
    : WebApplicationFactory<Program>, IDisposable
{
    internal string StoragePath { get; set; }

    public DefaultWebApplicationFactory()
    {
        StoragePath = Path.Combine(Path.GetTempPath(), CryptoString.NewValue());
        Directory.CreateDirectory(StoragePath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            var webConfig = new Dictionary<string, string?>()
            {
                { "AcmeFileStore:BasePath", StoragePath }
            };

            config.AddInMemoryCollection(webConfig);
        });

        builder.ConfigureServices(services =>
        {
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (Directory.Exists(StoragePath))
        {
            Directory.Delete(StoragePath, true);
        }
    }
}