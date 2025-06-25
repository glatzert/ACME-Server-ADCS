using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

using AlternativeNames = Th11s.ACMEServer.Services.X509.AlternativeNames;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

public class CSRValidator(
    IOptionsSnapshot<ProfileConfiguration> profileConfigurationOptions,
    ILogger<CSRValidator> logger
    ) : ICSRValidator
{
    private readonly IOptionsSnapshot<ProfileConfiguration> _profileConfigurationOptions = profileConfigurationOptions;
    private readonly ILogger<CSRValidator> _logger = logger;

    public Task<AcmeValidationResult> ValidateCsrAsync(Order order, CancellationToken cancellationToken)
    {
        CSRValidationContext validationContext;
        try
        {
            var profileConfiguration = _profileConfigurationOptions.Get(order.Profile.Value);
            validationContext = new CSRValidationContext(order, profileConfiguration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validation of CSR failed with exception.");
            return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CSR could not be read.")));
        }

        try
        {
            var publicKeyValidator = new ExpectedPublicKeyValidator();
            if (!publicKeyValidator.IsValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid public key.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "Public Key Invalid.")));
            }

            var sanValidator = new AlternativeNameValidator(_logger);
            if (!sanValidator.AreAllAlternateNamesValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "SAN Invalid.")));
            }

            if (!IsSubjectNameValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid CN.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CN Invalid.")));
            }

            // ACME states that all identifiers must be present in either CN or SAN.
            if (!validationContext.AreAllIdentifiersUsed())
            {
                _logger.LogDebug("CSR validation failed. Not all identifiers where present in either CN or SAN");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "Missing identifiers in CN or SAN.")));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Validation of CSR failed with exception.");
            return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CSR could not be read.")));
        }

        _logger.LogDebug("CSR Validation succeeded.");
        return Task.FromResult(AcmeValidationResult.Success());
    }


    #region Subject Name Validation
    private bool IsSubjectNameValid(CSRValidationContext validationContext)
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


    private bool IsCommonNameValid(string commonName, CSRValidationContext validationContext)
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

