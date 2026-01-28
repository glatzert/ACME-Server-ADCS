using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Services.CsrValidation;

internal class ExpectedPublicKeyValidator(ILogger logger)
{
    private readonly ILogger _logger = logger;

    internal void ValidateExpectedPublicKey(CsrValidationContext validationContext, string? expectedPublicKey, CertificateRequest certificateRequest)
    {
        // Check if there are expected public keys, if not we can skip the validation
        if (string.IsNullOrEmpty(expectedPublicKey))
        {
            _logger.LogDebug("The validation context did not contain expected public keys. Skipping validation");
            return;
        }

        // Check if the expected public key matches the public key in the CSR
        var publicKey = Convert.ToBase64String(certificateRequest.PublicKey.ExportSubjectPublicKeyInfo());

        var isValid = string.Equals(publicKey, expectedPublicKey, StringComparison.Ordinal);
        _logger.LogInformation("Validated expectedPublicKey against certificate request. Result: {isValid}", isValid);

        if (isValid)
        {
            validationContext.SetPublicKeyUsed();
        }
    }
}