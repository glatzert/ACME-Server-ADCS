using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using Windows.Win32;
using Windows.Win32.Security.Cryptography.Certificates;

namespace Th11s.ACMEServer.CertProvider.ADCS;

public sealed class CertificateIssuer : ICertificateIssuer
{
    private const int CR_OUT_BASE64 = 0x1;
    private const int CR_OUT_CHAIN = 0x100;

    private readonly IProfileProvider _profileProvider;
    private readonly ILogger<CertificateIssuer> _logger;

    public CertificateIssuer(IProfileProvider profileProvider, ILogger<CertificateIssuer> logger)
    {
        _profileProvider = profileProvider;
        _logger = logger;
    }

    public Task<(X509Certificate2Collection? Certificates, AcmeError? Error)> IssueCertificateAsync(ProfileName profile, string csr, CancellationToken cancellationToken)
    {
        _logger.TryIssueCertificate(csr);
        var result = (Certificates: (X509Certificate2Collection?)null, Error: (AcmeError?)null);

        if (!_profileProvider.TryGetProfileConfiguration(profile, out var profileConfiguration))
        {
            _logger.ProfileConfigurationNotFound(profile.Value);
            return Task.FromResult(((X509Certificate2Collection?)null, (AcmeError?)AcmeErrors.ServerInternal()));
        }

        try
        {
            var certRequest = CCertRequest.CreateInstance<ICertRequest>();
            var attributes = $"CertificateTemplate:{profileConfiguration.ADCSOptions.TemplateName}";

            using var configHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(profileConfiguration.ADCSOptions.CAServer));
            using var csrHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(csr));
            using var attributesHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(attributes));

            certRequest.Submit((int)CERT_IMPORT_FLAGS.CR_IN_BASE64, csrHandle, attributesHandle, configHandle, out var submitResponseCode);

            if (submitResponseCode == 3)
            {
                certRequest.GetCertificate(CR_OUT_BASE64 | CR_OUT_CHAIN, out var responseHandle);
                var issuerResponse = Marshal.PtrToStringBSTR(responseHandle.DangerousGetHandle());
                var issuerResponseBytes = Convert.FromBase64String(issuerResponse);

                responseHandle.Dispose();

                var issuerSignedCms = new SignedCms();
                issuerSignedCms.Decode(issuerResponseBytes);
                result.Certificates = issuerSignedCms.Certificates;

                _logger.CertificateIssued();
            }
            else
            {
                _logger.FailedIssuingCertificate(profileConfiguration.ADCSOptions.CAServer, profileConfiguration.ADCSOptions.TemplateName);
                _logger.CertificateIssuanceResponseCode(submitResponseCode);

                result.Error = AcmeErrors.ServerInternal("Certificate Issuance failed. Contact Administrator.");
            }
        }
        catch (Exception ex)
        {
            _logger.FailedIssuingCertificate(profileConfiguration.ADCSOptions.CAServer, profileConfiguration.ADCSOptions.TemplateName);
            _logger.CertificateIssuanceException(ex);
            result.Error = AcmeErrors.ServerInternal("Certificate Issuance failed. Contact Administrator");
        }

        return Task.FromResult(result);
    }

    public Task RevokeCertificateAsync(ProfileName profile, X509Certificate2 certificate, int? reason, CancellationToken cancellationToken)
    {
        _logger.AttemptRevokeCertificate(certificate.SerialNumber);
        if (!_profileProvider.TryGetProfileConfiguration(profile, out var profileConfiguration))
        {
            _logger.ProfileConfigurationNotFound(profile.Value);
            return Task.FromResult(((X509Certificate2Collection?)null, (AcmeError?)AcmeErrors.ServerInternal()));
        }

        try
        {

            using var configHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(profileConfiguration.ADCSOptions.CAServer));
            using var serialNumberHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(certificate.SerialNumber));

            var certAdmin = CCertAdmin.CreateInstance<ICertAdmin>();

            certAdmin.RevokeCertificate(configHandle, serialNumberHandle, reason ?? 0, 0);
            _logger.CertificateRevoked(certificate.SerialNumber);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.FailedRevokingCertificate(certificate.SerialNumber, profileConfiguration.ADCSOptions.CAServer);
            _logger.CertificateRevocationException(ex);
            throw AcmeErrors.ServerInternal("Revokation failed. Contact Administrator").AsException();
        }
    }
}
