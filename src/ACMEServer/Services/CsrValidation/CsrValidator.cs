using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Services.Asn1;
using Th11s.ACMEServer.Services.CertificateSigningRequest;
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


        var identifiers = order.Identifiers.ToList();
        var alternativeNames = certificateRequest.CertificateExtensions.GetSubjectAlternativeNames();
        var expectedPublicKeys = order.Authorizations.Select(x => x.Identifier.GetExpectedPublicKey()!).Where(x => x is not null);
        var subjectName = certificateRequest.SubjectName;

        var validationContext = new CsrValidationContext(identifiers, alternativeNames, expectedPublicKeys, subjectName);


        try
        {
            var publicKeyValidator = new ExpectedPublicKeyValidator(_logger);
            publicKeyValidator.ValidateExpectedPublicKey(validationContext, certificateRequest);
            if (!validationContext.IsExpectedPublicKeyUsed())
            {
                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("Public key did not match expected key."));
            }

            var sanValidator = new AlternativeNameValidator(_logger);
            if (!sanValidator.AreAllAlternateNamesValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("SAN Invalid."));
            }

            if (!IsSubjectNameValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid CN.");
                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("CN Invalid."));
            }

            // ACME states that all identifiers must be present in either CN or SAN.
            if (!validationContext.AreAllIdentifiersUsed())
            {
                _logger.LogDebug("CSR validation failed. Not all identifiers where present in either CN or SAN");
                return AcmeValidationResult.Failed(AcmeErrors.BadCSR("Missing identifiers in CN or SAN."));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Validation of CSR failed with exception.");
            return AcmeValidationResult.Failed(AcmeErrors.BadCSR("CSR could not be read."));
        }

        _logger.LogDebug("CSR Validation succeeded.");
        return AcmeValidationResult.Success();
    }


    #region Subject Name Validation
    private bool IsSubjectNameValid(CsrValidationContext validationContext)
    {
        // an empty subject is always acceptable
        if (validationContext.SubjectName == null)
            return true;

        // having no common name is always acceptable
        if (validationContext.CommonNames == null || validationContext.CommonNames.Count == 0)
            return true;

        // all common names need to be valid identifiers from the order OR match any allowed SAN
        foreach (var commonName in validationContext.CommonNames)
        {
            if (!IsCommonNameValid(commonName, validationContext))
            {
                _logger.LogInformation("Common Name '{CommonName}' could not be validated.", commonName);
                return false;
            }
        }

        return true;
    }


    private bool IsCommonNameValid(string commonName, CsrValidationContext validationContext)
    {
        // if the common name matches any identifier value, we can consider it valid
        var matchingIdentifiers = validationContext.Identifiers
                .Where(x => x.Value.Equals(commonName, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (matchingIdentifiers.Count != 0)
        {
            foreach (var identifier in matchingIdentifiers)
            {
                validationContext.SetIdentifierIsUsed(identifier);
            }

            return true;
        }


        // if the common name matches any SAN, we can consider it valid
        var hasMatchingSAN = validationContext.AlternativeNames
            .OfType<AlternativeNames.IStringConvertible>()
            .Any(san => san.AsString().Equals(commonName, StringComparison.Ordinal));


        return hasMatchingSAN;
    }
    #endregion
}

