using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TGIT.ACME.Protocol.IssuanceServices.ADCS;

if (args.Length != 3)
{
    Console.WriteLine($"Usage: ACME.CertProvider.ADCS.Tests.Manual.exe \"<CAConfig>\" \"<CertTemplate>\" \"<DNSName>\"");
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

var acdsOptions = new Microsoft.Extensions.Options.OptionsWrapper<ADCSOptions>(
    new ADCSOptions
    {
        CAServer = caConfig,
        TemplateName = caTemplate,
        AllowCNSuffix = true,
    });

var issuer = new CertificateIssuer(acdsOptions);
var issuerResult = await issuer.IssueCertificate(csrPEM, default);

if(issuerResult.Error != null || issuerResult.Certificates == null)
{
    Console.WriteLine(issuerResult.Error?.Detail ?? "Certificate was null, but there was no Error");
    return;
}

var certificateCollection = new X509Certificate2Collection();
certificateCollection.Import(issuerResult.Certificates);

var stringBuilder = new StringBuilder();
foreach (var certificate in certificateCollection)
{
    var certPem = PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert));
    stringBuilder.AppendLine(new string(certPem));
}

Console.WriteLine(stringBuilder.ToString());
Console.ReadLine();