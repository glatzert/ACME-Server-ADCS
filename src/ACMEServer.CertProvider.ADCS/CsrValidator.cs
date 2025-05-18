using CERTENROLLLib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.CertProvider.ADCS;

public class CSRValidator : ICSRValidator
{
    private readonly IOptions<ADCSOptions> _options;
    private readonly ILogger<CSRValidator> _logger;

    public CSRValidator(
        IOptions<ADCSOptions> options,
        ILogger<CSRValidator> logger)
    {
        _options = options;
        _logger = logger;
    }


    public Task<AcmeValidationResult> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Attempting validation of CSR {csr}");
        try
        {
            var request = new CX509CertificateRequestPkcs10();

            request.InitializeDecode(csr, EncodingType.XCN_CRYPT_STRING_BASE64);
            request.CheckSignature();

            var validationContext = CSRValidationContext.FromRequestAndOrder(request, order);

            var publicKeyValidator = new ExpectedPublicKeyValidator();
            if (!publicKeyValidator.IsValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid public key.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "Public Key Invalid.")));
            }

            var subjectValidator = new SubjectValidator();
            if (!subjectValidator.IsValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid CN.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CN Invalid.")));
            }

            var sanValidator = new AlternateNameValidator();
            if (!sanValidator.AreAllAlternateNamesValid(validationContext))
            {
                _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "SAN Invalid.")));
            }

            // ACME states that all identifiers must be present in either CN or SAN.
            if (!validationContext.AreAllIdentifiersValid())
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
}
