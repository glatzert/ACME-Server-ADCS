using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Services.Asn1;
using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CsrValidation;

public class CsrValidator(
    IOptionsSnapshot<ProfileConfiguration> profileConfigurationOptions,
    ILogger<CsrValidator> logger
    ) : ICsrValidator
{
    private readonly IOptionsSnapshot<ProfileConfiguration> _profileConfigurationOptions = profileConfigurationOptions;
    private readonly ILogger<CsrValidator> _logger = logger;

    public async Task<AcmeValidationResult> ValidateCsrAsync(Order order, CancellationToken cancellationToken)
    {
        using var ls = _logger.BeginScope("Running CSR validation for order '{OrderId}' of account '{AccountId}'", order.OrderId, order.AccountId);

        // The order tells us which profile to use for validation,
        // if it's null (which can happen, if the profile was renamed after order creation)
        // we have an internal server error.
        var profileConfiguration = _profileConfigurationOptions.Get(order.Profile.Value);
        if (profileConfiguration == null)
        {
            _logger.LogError("Profile configuration for profile '{Profile}' not found.", order.Profile.Value);
            return AcmeValidationResult.Failed(AcmeErrors.ServerInternal());
        }

        // Empty CSRs are not valid obviously.
        if (string.IsNullOrWhiteSpace(order.CertificateSigningRequest))
        {
            _logger.LogWarning("Certifcate signing request was null or empty.");
            return AcmeValidationResult.Failed(AcmeErrors.BadCSR("Certifcate signing request is empty."));
        }

        // Try to load the CSR, if it fails, we return a bad request.
        CertificateRequest certificateRequest;
        try
        {
            certificateRequest = CertificateRequest.LoadSigningRequest(
                Base64UrlTextEncoder.Decode(order.CertificateSigningRequest),
                HashAlgorithmName.SHA256, // we'll not sign the request, so this is more a placeholder than anything else
                CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions // this enables loading of extensions, which is required for SAN validation
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Certifcate signing request could not be decoded.");
            return AcmeValidationResult.Failed(AcmeErrors.BadCSR("Certifcate signing request could not be read or was not signed properly."));
        }


        var identifiers = order.Identifiers.ToArray();
        var alternativeNames = certificateRequest.CertificateExtensions.GetSubjectAlternativeNames();
        var commonNames = certificateRequest.SubjectName.GetCommonNames().ToArray();

        var validationContext = new CsrValidationContext(identifiers, alternativeNames, order.ExpectedPublicKey, commonNames);

        return ValidateCertificateRequestProperties(validationContext, profileConfiguration, certificateRequest, identifiers, alternativeNames, order.ExpectedPublicKey, commonNames);
    }

    internal AcmeValidationResult ValidateCertificateRequestProperties(
        CsrValidationContext validationContext, 
        ProfileConfiguration profileConfiguration, 
        CertificateRequest certificateRequest, 
        Identifier[] identifiers, 
        AlternativeNames.GeneralName[] alternativeNames, 
        string? expectedPublicKey, 
        string[] commonNames)
    {
        try
        {
            var publicKeyValidator = new ExpectedPublicKeyValidator(_logger);
            publicKeyValidator.ValidateExpectedPublicKey(validationContext, expectedPublicKey, certificateRequest);
            if (!validationContext.IsExpectedPublicKeyUsed())
            {
                _logger.LogWarning("CSR validation failed: Public key did not match expected key.");
                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("Public key did not match expected key."));
            }

            var sanValidator = new AlternativeNameValidator(_logger);
            sanValidator.ValidateAlternativeNamesAndIdentifierUsage(validationContext, profileConfiguration, alternativeNames, identifiers);
            if (!validationContext.AreAllAlternativeNamesValid())
            {
                var invalidAlternativeNames = alternativeNames
                .Where(x => !validationContext.IsAlternativeNameValid(x))
                .Select(x => x.ToString())
                .ToArray();

                _logger.LogWarning("CSR validation failed: Not all subject alternative names are valid. Invalid SANs: {InvalidAlternativeNames}", string.Join(", ", invalidAlternativeNames));
                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("SAN Invalid."));
            }

            var commonNameValidator = new CommonNameValidator(_logger);
            commonNameValidator.ValidateCommonNamesAndIdentifierUsage(validationContext, commonNames, identifiers, alternativeNames);
            if (!validationContext.AreAllCommonNamesValid())
            {
                var invalidCommonNames = commonNames
                   .Where(x => !validationContext.IsCommonNameValid(x))
                   .Select(x => x.ToString())
                   .ToArray();

                _logger.LogWarning("CSR validation failed: Not all common names are valid. Invalid CNs: {InvalidCommonNames}", string.Join(", ", invalidCommonNames));
                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("CN Invalid."));
            }

            // ACME states that all identifiers must be present in either CN or SAN.
            if (!validationContext.AreAllIdentifiersUsed())
            {
                var unusedIdentifiers = identifiers
                    .Where(x => !validationContext.IsIdentifierUsed(x))
                    .Select(x => x.ToString())
                    .ToArray();

                _logger.LogWarning("CSR validation failed: Not all identifiers were used in the CSR. Unused identifiers: {UnusedIdentifiers}", string.Join(", ", unusedIdentifiers));

                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("Missing identifiers in CN or SAN."));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Validation of CSR failed with exception.");
            return AcmeValidationResult.Failed(AcmeErrors.BadCSR("CSR validation failed."));
        }

        _logger.LogDebug("CSR Validation succeeded.");
        return AcmeValidationResult.Success();
    }
}

