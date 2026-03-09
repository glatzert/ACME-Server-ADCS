using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
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
    private readonly IPublicKeyAnalyzer _publicKeyAnalyzer;
    private readonly ILogger<CertificateIssuer> _logger;

    public CertificateIssuer(
        IProfileProvider profileProvider, 
        IPublicKeyAnalyzer publicKeyAnalyzer,
        ILogger<CertificateIssuer> logger)
    {
        _profileProvider = profileProvider;
        _publicKeyAnalyzer = publicKeyAnalyzer;
        _logger = logger;
    }

    public async Task<(X509Certificate2Collection? Certificates, AcmeError? Error)> IssueCertificateAsync(ProfileName profile, string csr, CancellationToken cancellationToken)
    {
        _logger.TryIssueCertificate(csr);
        var result = (Certificates: (X509Certificate2Collection?)null, Error: (AcmeError?)null);

        if (!_profileProvider.TryGetProfileConfiguration(profile, out var profileConfiguration))
        {
            _logger.ProfileConfigurationNotFound(profile.Value);
            return (null, (AcmeError?)AcmeErrors.ServerInternal());
        }

        var certificateTemplate = await SelectCertificateTemplate(csr, profileConfiguration.ADCSOptions, cancellationToken);

        try
        {
            var certRequest = CCertRequest.CreateInstance<ICertRequest>();
            var attributes = $"CertificateTemplate:{certificateTemplate}";

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

        return result;
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


    private async Task<string> SelectCertificateTemplate(string certificateSigningRequest, ADCSOptions adcsOptions, CancellationToken ct)
    {
        if (adcsOptions.Templates is null or { Length: 0 })
        {
            return adcsOptions.TemplateName;
        }

        var publicKeyInfo = await _publicKeyAnalyzer.AnalyzePublicKeyAsync(certificateSigningRequest, ct);
        if (publicKeyInfo == null)
        {
            //TODO: _logger.FailedAnalyzingPublicKey(certificateSigningRequest);
            return adcsOptions.TemplateName;
        }

        var keyTypeMatchingTemplates = adcsOptions.Templates
            .Where(x => x.PublicKeyAlgorithms.Any(pk => pk.Equals(publicKeyInfo.KeyType, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var keyAndSizeMatchingTemplates = keyTypeMatchingTemplates
            .Where(x => x.KeySizes?.Any(ks => ks == publicKeyInfo.KeySize) == true)
            .ToList();

        if (keyAndSizeMatchingTemplates.Count > 0) 
        {
            if (keyAndSizeMatchingTemplates.Count > 1)
            {
                // TODO: _logger.MultipleMatchingTemplates(certificateSigningRequest, keyAndSizeMatchingTemplates.Select(t => t.Name));
            }

            return keyAndSizeMatchingTemplates.First().TemplateName;
        }

        // We'll get here if we found no matching template with the correct key size.
        var fallbackTemplates = keyTypeMatchingTemplates
            .Where(x => x.KeySizes?.Any() != true)
            .ToList();

        if (fallbackTemplates.Count > 0)
        {
            if (fallbackTemplates.Count > 1)
            {
                // TODO: _logger.FallbackTemplates(certificateSigningRequest, fallbackTemplates.Select(t => t.Name));
            }

            return fallbackTemplates.First().TemplateName;
        }

        // We'll get here if we found no matching template with the correct key size, and no fallback template without key size restrictions.
        return adcsOptions.TemplateName;
    }
}
