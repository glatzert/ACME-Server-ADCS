using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using CertCli = CERTCLILib;

namespace Th11s.ACMEServer.CertProvider.ADCS;

public sealed class CertificateIssuer : ICertificateIssuer
{
    private const int CR_IN_BASE64 = 0x1;
    private const int CR_OUT_BASE64 = 0x1;
    private const int CR_OUT_CHAIN = 0x100;

    private readonly IOptionsSnapshot<ProfileConfiguration> _options;
    private readonly ILogger<CertificateIssuer> _logger;

    public CertificateIssuer(IOptionsSnapshot<ProfileConfiguration> options, ILogger<CertificateIssuer> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<(X509Certificate2Collection? Certificates, AcmeError? Error)> IssueCertificate(ProfileName profile, string csr, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Try to issue certificate for CSR: {csr}", csr);
        var result = (Certificates: (X509Certificate2Collection?)null, Error: (AcmeError?)null);

        var options = _options.Get(profile.Value);

        try
        {
            var certRequest = new CertCli.CCertRequest();
            var attributes = $"CertificateTemplate:{options.ADCSOptions.TemplateName}";
            var submitResponseCode = certRequest.Submit(CR_IN_BASE64, csr, attributes, options.ADCSOptions.CAServer);

            if (submitResponseCode == 3)
            {
                var issuerResponse = certRequest.GetCertificate(CR_OUT_BASE64 | CR_OUT_CHAIN);
                var issuerResponseBytes = Convert.FromBase64String(issuerResponse);

                var issuerSignedCms = new SignedCms();
                issuerSignedCms.Decode(issuerResponseBytes);
                result.Certificates = issuerSignedCms.Certificates

                _logger.LogDebug("Certificate has been issued.");
            }
            else
            {
                _logger.LogError("Tried using Config {CAServer} and Template {TemplateName} to issue certificate", options.ADCSOptions.CAServer, options.ADCSOptions.TemplateName);
                _logger.LogError("Certificate could not be issued. ResponseCode: {submitResponseCode}.", submitResponseCode);

                result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Tried using Config {CAServer} and Template {TemplateName} to issue certificate", options.ADCSOptions.CAServer, options.ADCSOptions.TemplateName);
            _logger.LogError(ex, "Exception has been raised during certificate issuance.");
            result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator");
        }

        return Task.FromResult(result);
    }
}
