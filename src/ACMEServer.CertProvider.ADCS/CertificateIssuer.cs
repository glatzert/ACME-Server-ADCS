using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using Windows.Win32;
using Windows.Win32.Security.Cryptography.Certificates;
using PublicKeyInfo = Th11s.ACMEServer.Model.PublicKeyInfo;

namespace Th11s.ACMEServer.CertProvider.ADCS;

public sealed class CertificateIssuer : ICertificateIssuer
{
    private const int CR_OUT_BASE64 = 0x1;
    private const int CR_OUT_CHAIN = 0x100;

    private readonly IProfileProvider _profileProvider;
    private readonly IPublicKeyAnalyzer _publicKeyAnalyzer;
    private readonly ILogger<CertificateIssuer> _logger;

    internal static class MetadataKeys
    {
        public const string CAServer = "CAServer";
        public const string TemplateName = "TemplateName";
    }

    public CertificateIssuer(
        IProfileProvider profileProvider,
        IPublicKeyAnalyzer publicKeyAnalyzer,
        ILogger<CertificateIssuer> logger)
    {
        _profileProvider = profileProvider;
        _publicKeyAnalyzer = publicKeyAnalyzer;
        _logger = logger;
    }

    public async Task<CertificateIssuanceResult> IssueCertificateAsync(ProfileName profile, string csr, CancellationToken cancellationToken)
    {
        _logger.TryIssueCertificate(csr);
        var result = (Certificates: (X509Certificate2Collection?)null, Error: (AcmeError?)null);

        if (!_profileProvider.TryGetProfileConfiguration(profile, out var profileConfiguration))
        {
            _logger.ProfileConfigurationNotFound(profile.Value);
            return new(AcmeErrors.ServerInternal());
        }

        var publicKeyInfo = await _publicKeyAnalyzer.AnalyzePublicKeyAsync(csr, cancellationToken)
            ?? new(null, null);

        var caConfig = await SelectCAConfig(publicKeyInfo, profileConfiguration.GetCertificateServices(), cancellationToken);
        if (caConfig is null)
        {
            return new(AcmeErrors.ServerInternal("No suitable certificate template found. Contact Administrator."));
        }

        _logger.SelectedCAConfig(publicKeyInfo.KeyType, publicKeyInfo.KeySize, caConfig.CAServer, caConfig.TemplateName);


        try
        {
            var certRequest = CCertRequest.CreateInstance<ICertRequest>();
            var attributes = $"CertificateTemplate:{caConfig.TemplateName}";

            using var configHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(caConfig.CAServer));
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
                return new(result.Certificates, new()
                {
                    { MetadataKeys.CAServer, caConfig.CAServer },
                    { MetadataKeys.TemplateName, caConfig.TemplateName }
                });
            }
            else
            {
                _logger.FailedIssuingCertificate(caConfig.CAServer, caConfig.TemplateName);
                _logger.CertificateIssuanceResponseCode(submitResponseCode);

                return new(AcmeErrors.ServerInternal("Certificate Issuance failed. Contact Administrator."));
            }
        }
        catch (Exception ex)
        {
            _logger.FailedIssuingCertificate(caConfig.CAServer, caConfig.TemplateName);
            _logger.CertificateIssuanceException(ex);
            return new(AcmeErrors.ServerInternal("Certificate Issuance failed. Contact Administrator"));
        }
    }


    public Task RevokeCertificateAsync(ProfileName profile, X509Certificate2 certificate, Dictionary<string, string> issuanceMetadata, int? reason, CancellationToken cancellationToken)
    {
        _logger.AttemptRevokeCertificate(certificate.SerialNumber);

        if (!issuanceMetadata.TryGetValue(MetadataKeys.CAServer, out var caServer))
        {
            // If there's no metadata (yet), we'll try to get the CA server from the profile configuration.
            // This is a fallback for certificates issued before we started storing the CA server in the metadata.
            if (!_profileProvider.TryGetProfileConfiguration(profile, out var profileConfiguration))
            {
                _logger.ProfileConfigurationNotFound(profile.Value);
                throw AcmeErrors.ServerInternal("Profile configuration not found. Contact Administrator").AsException();
            }

            caServer = profileConfiguration.ADCSOptions?.CAServer ?? profileConfiguration.CertificateServices?.FirstOrDefault()?.CAServer;
        }

        if (string.IsNullOrWhiteSpace(caServer))
        {
            _logger.FailedRevokingCertificate(certificate.SerialNumber, "CAServer could not be determined.");
            throw AcmeErrors.ServerInternal("CA Server not found. Contact Administrator").AsException();
        }

        try
        {
            using var configHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(caServer));
            using var serialNumberHandle = new SysFreeStringSafeHandle(Marshal.StringToBSTR(certificate.SerialNumber));

            var certAdmin = CCertAdmin.CreateInstance<ICertAdmin>();

            certAdmin.RevokeCertificate(configHandle, serialNumberHandle, reason ?? 0, 0);
            _logger.CertificateRevoked(certificate.SerialNumber);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.FailedRevokingCertificate(certificate.SerialNumber, caServer);
            _logger.CertificateRevocationException(ex);
            throw AcmeErrors.ServerInternal("Revokation failed. Contact Administrator").AsException();
        }
    }


    private async Task<CAConfig?> SelectCAConfig(PublicKeyInfo publicKeyInfo, ADCSOptions[] adcsOptions, CancellationToken ct)
    {
        // Build a simple list of all combinations of public key algorithm and key size specified in the ADCS options, so we can easily look up the correct template based on the public key info in the CSR.
        var expandedADCSOptions = new List<(string? KeyAlgorithm, int? KeySize, CAConfig CAConfig)>();
        foreach(var option in adcsOptions)
        {
            foreach (var keyAlgorithm in option.PublicKeyAlgorithms)
            {
                ExpandOnKeySize(expandedADCSOptions, option, keyAlgorithm);
            }

            if (option.PublicKeyAlgorithms.Length == 0)
            {
                ExpandOnKeySize(expandedADCSOptions, option, null);
            }
        }

        var configLookup = expandedADCSOptions
            .ToLookup(
                x => (x.KeyAlgorithm, x.KeySize),
                x => x.CAConfig
            );

        var fullMatch = configLookup[(publicKeyInfo.KeyType, publicKeyInfo.KeySize)].ToList();
        if (fullMatch.Count > 0)
        {
            if (fullMatch.Count > 1)
            {
                _logger.MultipleCertificateServicesMatchedPublicKeyInfo(publicKeyInfo.KeyType, publicKeyInfo.KeySize);
            }

            return fullMatch.First();
        }

        _logger.CouldNotMatchPublicKeyTypeAndSize(publicKeyInfo.KeyType, publicKeyInfo.KeySize);


        var keyTypeMatch = configLookup[(publicKeyInfo.KeyType, null)].ToList();
        if (keyTypeMatch.Count > 0)
        {
            if (keyTypeMatch.Count > 1)
            {
                _logger.MultipleCertificateServicesMatchedPublicKeyAlgorithm(publicKeyInfo.KeyType);
            }

            return keyTypeMatch.First();
        }

        _logger.CouldNotMatchPublicKeyType(publicKeyInfo.KeyType);


        var keySizeMatch = configLookup[(null, publicKeyInfo.KeySize)].ToList();
        if (keySizeMatch.Count > 0)
        {
            if (keySizeMatch.Count > 1)
            {
                _logger.MultipleCertificateServicesMatchedPublicKeySize(publicKeyInfo.KeySize);
            }

            return keySizeMatch.First();
        }

        _logger.CouldNotMatchPublicKeySize(publicKeyInfo.KeySize);


        var fallbackOptions = configLookup[(null, null)].ToList();
        if (fallbackOptions.Count > 0)
        {
            if (fallbackOptions.Count > 1)
            {
                _logger.MultipleCertificateServicesMatchedAsFallback();
            }

            return fallbackOptions.First();
        }

        _logger.NoCertificateServiceMatched(publicKeyInfo.KeyType, publicKeyInfo.KeySize);
        return null;
    }


    static void ExpandOnKeySize(List<(string? KeyAlgorithm, int? KeySize, CAConfig Options)> expandedADCSOptions, ADCSOptions option, string? keyAlgorithm)
    {
        foreach (var keySize in option.KeySizes)
        {
            expandedADCSOptions.Add((keyAlgorithm, keySize, new CAConfig(option.CAServer, option.TemplateName)));
        }

        if (option.KeySizes.Length == 0)
        {
            expandedADCSOptions.Add((keyAlgorithm, null, new CAConfig(option.CAServer, option.TemplateName)));
        }
    }

    private record CAConfig(string CAServer, string TemplateName);
}
