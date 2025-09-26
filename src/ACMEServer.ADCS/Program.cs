using ACMEServer.Storage.FileSystem.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Th11s.ACMEServer.AspNetCore;
using Th11s.ACMEServer.CertProvider.ADCS.Extensions;

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
                Indented = false
            },
            EntrySeparator = "\n",
        },
        x => {
            x.RootPath = builder.Configuration.GetValue<string>("AcmeFileStore:BasePath");
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
            opt.KnownNetworks.Clear();

            foreach (var network in knownNetworks)
            {
                opt.KnownNetworks.Add(IPNetwork.Parse(network));
            }
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