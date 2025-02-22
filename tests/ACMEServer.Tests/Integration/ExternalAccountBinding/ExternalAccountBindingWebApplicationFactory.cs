using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Services;

namespace ACMEServer.Tests.Integration.ExternalAccountBinding;

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
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            ctx.Configuration["ACMEServer:ExternalAccountBinding:Required"] = "true";
            ctx.Configuration["ACMEServer:ExternalAccountBinding:MACRetrievalUrl"] = new Uri(EABServer.GetTestServer().BaseAddress, "/get/the/mac/{kid}").ToString();
            ctx.Configuration["ACMEServer:ExternalAccountBinding:SuccessSignalUrl"] = new Uri(EABServer.GetTestServer().BaseAddress, "/success/{kid}").ToString();
            ctx.Configuration["ACMEServer:ExternalAccountBinding:FailedSignalUrl"] = new Uri(EABServer.GetTestServer().BaseAddress, "/failure/{kid}").ToString();
        });

        builder.ConfigureServices(services =>
        {
            services.AddScoped<IExternalAccountBindingClient>(sp =>
            {
                var fakeClient = EABServer.GetTestClient();

                return new DefaultExternalAccountBindingClient(
                    fakeClient,
                    sp.GetRequiredService<IOptions<ACMEServerOptions>>());
            });
        });

        builder.UseEnvironment("Development");
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
                            endpoints.MapGet("/get/the/mac/{kid}", (string kid) =>
                            {
                                if (kid == "keyId")
                                {
                                    return Results.Text(Base64UrlEncoder.Encode(EABKey));
                                }

                                return Results.BadRequest();
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