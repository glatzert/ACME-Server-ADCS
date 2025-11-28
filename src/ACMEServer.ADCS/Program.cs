using ACMEServer.Storage.FileSystem.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Th11s.ACMEServer.AspNetCore;
using Th11s.ACMEServer.CertProvider.ADCS.Extensions;
using Th11s.ACMEServer.CLI.CertificateIssuance;
using Th11s.ACMEServer.CLI.ConfigTool;

if (args.Length >= 1 && args[0] == "--config-tool")
{
    var configCreationTool = new ConfigCLI();
    await configCreationTool.RunAsync();
    return;
}

if (args.Length >= 1 && args[0] == "--test-issuance")
{
    var issuanceTestTool = new IssuanceTestCLI();
    await IssuanceTestCLI.RunAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Enables Windows Service hosting
builder.Host.UseWindowsService();

if (builder.Configuration.GetSection("Logging:File").Exists())
{
    builder.Logging.AddJsonFile(
        new ()
        {
            JsonWriterOptions = new()
            {
                Indented = false,
            },
            EntrySeparator = ""
        },
        x => {
            x.RootPath = builder.Environment.IsProduction()
                ? builder.Configuration.GetValue<string>("AcmeFileStore:BasePath")
                : Path.GetTempPath();
        }
    );
}

var services = builder.Services;
if (builder.Configuration.GetValue("Logging:EnableHttpLogging", false))
{
    services.AddHttpLogging(opt => { });
}

//Configure forwarded headers, if the config section exists
var forwardedHeadersSection = builder.Configuration.GetSection("ForwardedHeaders");
if (forwardedHeadersSection.Exists())
{
    services.Configure<ForwardedHeadersOptions>(opt =>
    {
        opt.ForwardedHeaders = ForwardedHeaders.All;
        forwardedHeadersSection.Bind(opt);

        var knownNetworks = forwardedHeadersSection.GetSection("KnownNetworks").Get<string[]>();
        if (knownNetworks != null)
        {
#if NET10_0_OR_GREATER
            opt.KnownIPNetworks.Clear();
            foreach (var network in knownNetworks)
            {
                opt.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
#else
            opt.KnownNetworks.Clear();
            foreach (var network in knownNetworks)
            {
                opt.KnownNetworks.Add(IPNetwork.Parse(network));
            }
#endif
        }

        var knownProxies = forwardedHeadersSection.GetSection("KnownProxies").Get<string[]>();
        if (knownProxies != null)
        {
            opt.KnownProxies.Clear();
            foreach (var proxy in knownProxies)
            {
                opt.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
            }
        }
    });
}

services.AddRouting();

services.AddHttpContextAccessor();
services.AddACMEServer(builder.Configuration, "AcmeServer");
services.AddACMEFileStore("AcmeFileStore");
services.AddADCSIssuer();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (forwardedHeadersSection.Exists())
{
    app.Logger.LogInformation("Forwarded headers have been configured and added to the execution pipeline.");
    app.UseForwardedHeaders();
}

if (app.Configuration.GetValue("Logging:EnableHttpLogging", false))
{
    app.UseHttpLogging();
}

app.UseAcmeServer();
app.Run();

public partial class Program { }