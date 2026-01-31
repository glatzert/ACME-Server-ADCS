using ACMEServer.CertProvider.ADCS.Tests;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

using Th11s.ACMEServer.CertProvider.ADCS;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Tests.Utils;
using Th11s.ACMEServer.Tests.Utils.Fakes;

using var loggerFactory = LoggerFactory.Create(log => log.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

// get config and template
var certificateTemplates = ActiveDirectoryUtility.GetEnrollmentServiceCollection();
var (config, template) = PromptForTemplate(certificateTemplates);

if (config == null || template == null)
{
    return;
}

var certificateIssuer = CreateCertificateIssuer(config, template, loggerFactory.CreateLogger<CertificateIssuer>());
var dnsName = PromptForDnsName();

var algorithm = PromptForAlgorithm();

// create CSR
var certificateRequest = new CertificateRequestBuilder()
    .WithDefaultSubjectSuffix()
    .WithCommonName(dnsName)
    .WithDnsName(dnsName);

if (algorithm == "RSA") {
    certificateRequest = certificateRequest.WithPrivateKey(RSA.Create(4096));
}

var (certificates, error) = await certificateIssuer.IssueCertificateAsync(
    new("Default"), 
    certificateRequest.AsBase64Url(), 
    default);

if (error != null)
{
    logger.LogError(error.ToString());
    return;
}

var certificate = certificates!.GetLeafCertificate()!;
Console.WriteLine($"Issued certificate {certificate.SerialNumber}");

if(PromptForRevoke())
{
    await certificateIssuer.RevokeCertificateAsync(new("Default"), certificate, 1, default);
}


#region Local Functions
CertificateIssuer CreateCertificateIssuer(string configuration, string template, ILogger<CertificateIssuer> logger)
{
    return new CertificateIssuer(
        new FakeProfileProvider(
            new()
            {
                [new ProfileName("Default")] = new ProfileConfiguration()
                {
                    ADCSOptions = new()
                    {
                        CAServer = configuration,
                        TemplateName = template
                    },
                    SupportedIdentifiers = ["dns"]
                }
            }),
        logger
    );
}

static T? PromptForItem<T>(IList<T> items, string prompt, Func<T, string> elementDisplay)
{
    int? selection = null;

    do
    {

        Console.WriteLine();
        for (int i = 0; i < items.Count; ++i)
        {
            Console.WriteLine($"[{i + 1,3}] {elementDisplay(items[i])}");
        }
        Console.WriteLine("[  0]: Cancel");

        Console.WriteLine();
        Console.WriteLine(prompt);
        var userInput = Console.ReadLine();

        if (int.TryParse(userInput, out var parsedInput))
        {
            selection = parsedInput;
        }
    } while (!selection.HasValue || selection > items.Count || selection < 0);

    if (selection == 0)
    {
        return default;
    }

    return items[selection.Value - 1];
}

static (string? config, string? template) PromptForTemplate(IList<ADCertificationAuthority> authorities)
{
    var templates = authorities
        .SelectMany(
            x => x.CertificateTemplates,
            (x, y) => new { x.ConfigurationString, Template = y })
        .ToList();

    var item = PromptForItem(
        templates,
        "Select Template and CA",
        x => $"{x.ConfigurationString} - {x.Template}");

    if (item != null)
    {
        return (item.ConfigurationString, item.Template);
    }

    return (null, null);
}

static string PromptForAlgorithm()
{
    string[] items = ["RSA", "ECDSA"];
    var item = PromptForItem(
        items,
        "Select Key Algorithm",
        x => x);

    return item ?? "RSA";
}


static string PromptForDnsName()
{
    // get DNS name
    Console.WriteLine("Supply DNS name for testing [www.example.com]");
    var dnsName = Console.ReadLine() ?? "www.example.com";
    if (string.IsNullOrWhiteSpace(dnsName))
    {
        dnsName = "www.example.com";
    }

    return dnsName;
}

static bool PromptForRevoke()
{
    Console.WriteLine("Revoke certificate? (y/N)");
    var input = Console.ReadLine();
    return input != null && (input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase));
}
#endregion
