using ACMEServer.CertProvider.ADCS.Tests;
using ACMEServer.Tests.Utils;
using Microsoft.Extensions.Logging;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.AcmeServer.Tests.Fakes;
using Th11s.ACMEServer.CertProvider.ADCS;
using Th11s.ACMEServer.Model.Configuration;

var loggerFactory = LoggerFactory.Create(log => log.AddConsole());

var certificateTemplates = ActiveDirectoryUtility.GetEnrollmentServiceCollection();

// get config and template
var (config, template) = PromptForTemplate(certificateTemplates);
if (config == null)
{
    return;
}

// get DNS name
Console.WriteLine("Supply DNS name for testing [www.example.com]");
var dnsName = Console.ReadLine() ?? "www.example.com";
if (string.IsNullOrWhiteSpace(dnsName))
{
    dnsName = "www.example.com";
}

// create CSR
var certificateRequest = new CertificateRequestBuilder()
    .WithDefaultSubjectSuffix()
    .WithCommonName(dnsName)
    .WithPrivateKey(ECDsa.Create())
    .WithDnsName(dnsName)
    .AsBase64Url();

var certificateIssuer = new CertificateIssuer(
    new FakeOptionSnapshot<ProfileConfiguration>(new Dictionary<string, ProfileConfiguration>
    {
        ["Default"] = new ProfileConfiguration()
        {
            ADCSOptions = new()
            {
                CAServer = config,
                TemplateName = template
            },
            SupportedIdentifiers = ["dns"]
        }
    }),
    loggerFactory.CreateLogger<CertificateIssuer>()
);

var certificates = await certificateIssuer.IssueCertificate(new("Default"), certificateRequest, default);
Console.WriteLine(certificates.ToString());

#region Local Functions
(string? config, string? template) PromptForTemplate(IList<ADCertificationAuthority> authorities)
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

T? PromptForItem<T>(IList<T> items, string prompt, Func<T, string> elementDisplay)
{
    int? selection = null;

    do
    {
        
        Console.WriteLine();
        for (int i = 0; i < items.Count; ++i) {
            Console.WriteLine($"[{i+1,3}] {elementDisplay(items[i])}");
        }
        Console.WriteLine("[  0]: Cancel");

        Console.WriteLine();
        Console.WriteLine(prompt);
        var userInput = Console.ReadLine();

        if(int.TryParse(userInput, out var parsedInput))
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
#endregion
