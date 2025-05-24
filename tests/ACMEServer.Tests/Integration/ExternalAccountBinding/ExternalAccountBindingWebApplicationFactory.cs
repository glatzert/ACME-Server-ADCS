using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Services;

namespace Th11s.AcmeServer.Tests.Integration.ExternalAccountBinding;

public class ExternalAccountBindingWebApplicationFactory
    : DefaultWebApplicationFactory, IDisposable
{
    public static readonly byte[] EABKey;

    public IHost EABServer { get; }

    static ExternalAccountBindingWebApplicationFactory()
    {
        Random.Shared.NextBytes(EABKey = new byte[32]);
    }

    public ExternalAccountBindingWebApplicationFactory()
    {
        EABServer = GetFakeExternalBindingServer();
    }

    public new void Dispose()
    {
        EABServer?.Dispose();
        base.Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configKey = "ACMEServer:ExternalAccountBinding";
        var eabBaseUri = EABServer.GetTestServer().BaseAddress;

        var eabConfigurations = new Dictionary<string, string?>()
            {
                { $"{configKey}:Required", "true" },
                { $"{configKey}:MACRetrievalUrl", new Uri(eabBaseUri, "/get/the/mac/{kid}").ToString() },
                { $"{configKey}:SuccessSignalUrl", new Uri(eabBaseUri, "/success/{kid}").ToString() },
                { $"{configKey}:FailedSignalUrl", new Uri(eabBaseUri, "/failure/{kid}").ToString() },

                { $"{configKey}:Headers:0:Key", "Authorization" },
                { $"{configKey}:Headers:0:Value", "ApiKey TrustMeBro" }
            };

        var eabConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(eabConfigurations)
            .Build();

        builder.UseConfiguration(eabConfiguration);
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.AddScoped<IExternalAccountBindingClient>(sp =>
            {
                var fakeClient = EABServer.GetTestClient();

                return new DefaultExternalAccountBindingClient(
                    fakeClient,
                    sp.GetRequiredService<IOptions<ACMEServerOptions>>(),
                    NullLogger<DefaultExternalAccountBindingClient>.Instance);
            });
        });

        builder.UseEnvironment("Test");
    }

    internal IHost GetFakeExternalBindingServer()
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                    })
                    .Configure((ctx, app) =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/get/the/mac/{kid}", (HttpContext ctx, string kid) =>
                            {
                                if (kid == "keyId" && ctx.Request.Headers["Authorization"] == "ApiKey TrustMeBro")
                                {
                                    return Results.Text(Base64UrlEncoder.Encode(EABKey));
                                }

                                return Results.BadRequest("Test not okay");
                            });

                            endpoints.MapGet("/success/{kid}", (string kid) =>
                            {
                                return Results.Ok();
                            });

                            endpoints.MapGet("/failure/{kid}", (string kid) =>
                            {
                                return Results.Ok();
                            });
                        });
                        
                    });
            })
            .Start();


    }
}