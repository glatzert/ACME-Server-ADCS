using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using Th11s.ACMEServer.CertProvider.ADCS;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Extensions;

namespace Th11s.ACMEServer.CLI.CertificateIssuance;

public class IssuanceTestCLI
{
    public static async Task RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole(opt =>
            {
                opt.MaxQueueLength = 0; // This makes the logger synchronous
            });
        });

        var logger = loggerFactory.CreateLogger<IssuanceTestCLI>();

        var (caConfig, template) = CLIPrompt.PromptCAConfigAndTemplate();

        if (caConfig == null || template == null)
        {
            Console.WriteLine("No CA configuration or template selected, aborting.");
            return;
        }

        var identifierType = CLIPrompt.Select("Choose an identifier type", [IdentifierTypes.DNS, IdentifierTypes.IP], x => x);
        if (identifierType == null)
        {
            Console.WriteLine("No identifier type selected, aborting.");
            return;
        }

        var identifierValue = identifierType switch
        {
            IdentifierTypes.DNS => CLIPrompt.Hostname("Supply DNS name for testing (e.g www.example.com)"),
            IdentifierTypes.IP => CLIPrompt.String("Supply IP )address for testing (e.g 192.168.1.1)", x => IPAddress.TryParse(x, out _)),
            _ => null,
        };

        if (identifierValue == null)
        {
            Console.WriteLine("No identifier value supplied, aborting.");
            return;
        }

        var certificateIssuer = IssuanceTestCLI.CreateCertificateIssuer(caConfig, template, identifierType, loggerFactory.CreateLogger<CertificateIssuer>());
        var algorithm = CLIPrompt.Select("Select certificate algorithm", ["RSA", "ECDSA"], x => x);

        // create CSR
        var certificateRequest = new CertificateRequestBuilder()
            .WithDefaultSubjectSuffix()
            .WithCommonName(identifierValue);

        if (identifierType == IdentifierTypes.DNS)
        {
            certificateRequest = certificateRequest.WithDnsName(identifierValue);
        }
        else if (identifierType == IdentifierTypes.IP)
        {
            certificateRequest = certificateRequest.WithIpAddress(IPAddress.Parse(identifierValue));
        }

        if (algorithm == "RSA")
        {
            certificateRequest = certificateRequest.WithPrivateKey(RSA.Create(4096));
        }
        else
        {
            certificateRequest = certificateRequest.WithPrivateKey(ECDsa.Create(ECCurve.NamedCurves.nistP256));
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

        if (CLIPrompt.Bool("Revoke certificate?"))
        {
            await certificateIssuer.RevokeCertificateAsync(new("Default"), certificate, 1, default);
        }
    }

    private static CertificateIssuer CreateCertificateIssuer(string configuration, string template, string identifierType, ILogger<CertificateIssuer> logger)
    {
        return new CertificateIssuer(
            new OptionsSnapshot<ProfileConfiguration>(
                new Dictionary<string, ProfileConfiguration>
                {
                    ["Default"] = new ProfileConfiguration()
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
}