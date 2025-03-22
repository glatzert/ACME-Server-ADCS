using ACMEServer.Storage.FileSystem.Extensions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.AspNetCore;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.CertProvider.ADCS.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.GetValue("Logging:EnableFileLog", false))
{
    builder.Logging.AddFile(builder.Configuration.GetSection("Logging"));
}

// Enables Windows Service hosting
builder.Host.UseWindowsService();

var services = builder.Services;
if (builder.Configuration.GetValue("Logging:EnableHttpLogging", false))
{
    services.AddHttpLogging(opt => { });
}

services.AddRouting();
services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

services.AddHttpContextAccessor();
services.AddACMEServer(builder.Configuration, "AcmeServer");
services.AddACMEFileStore("AcmeFileStore");
services.AddADCSIssuer("ADCSIssuer");


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (app.Configuration.GetValue("Logging:EnableHttpLogging", false))
{
    app.UseHttpLogging();
}

app.UseRouting();

app.MapAcmeServer();
app.MapControllers();

app.Run();

public partial class Program { }