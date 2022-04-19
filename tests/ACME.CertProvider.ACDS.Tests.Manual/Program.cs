using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using TGIT.ACME.Protocol.IssuanceServices.ACDS;

if (args.Length != 3)
{
    Console.WriteLine($"Usage: ACME.CertProvider.ACDS.Tests.Manual.exe \"<CAConfig>\" \"<CertTemplate>\" \"<DNSName>\"");
    return;
}

var caConfig = args[0];
var caTemplate = args[1];
var dnsName = args[2];

var subjectName = $"CN={dnsName}";

var csp = RSA.Create(2048);
var csr = new CertificateRequest(subjectName, csp, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

var sanBuilder = new SubjectAlternativeNameBuilder();
sanBuilder.AddDnsName(dnsName);
csr.CertificateExtensions.Add(sanBuilder.Build());

var csrBytes = csr.CreateSigningRequest();
var csrPEM = $"{Convert.ToBase64String(csrBytes)}";

var acdsOptions = new Microsoft.Extensions.Options.OptionsWrapper<ACDSOptions>(
    new ACDSOptions
    {
        CAServer = caConfig,
        TemplateName = caTemplate,
        AllowCNSuffix = true,
    });

var issuer = new CertificateIssuer(acdsOptions);
var issuerResult = await issuer.IssueCertificate(csrPEM, default);

if(issuerResult.error != null || issuerResult.certificate == null)
{
    Console.WriteLine(issuerResult.error?.Detail ?? "Certificate was null, but there was no Error");
    return;
}

var signedCms = new SignedCms();
signedCms.Decode(issuerResult.certificate);

Console.WriteLine($"Certificates: {string.Join(", ", signedCms.Certificates.Select(x => x.Subject))}");