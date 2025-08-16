using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Services.CsrValidation;

internal class ExpectedPublicKeyValidator(ILogger logger)
{
    private readonly ILogger _logger = logger;

    internal void ValidateExpectedPublicKey(CsrValidationContext validationContext, CertificateRequest certificateRequest)
    {
        // Check if there are expected public keys, if not we can skip the validation
        if (validationContext.ExpectedPublicKeys.Count == 0)
        {
            _logger.LogDebug("The validation context did not contain expected public keys. Skipping validation");
            return;
        }

        // Check if there are more than one expected public keys, if so we cannot validate
        if (validationContext.ExpectedPublicKeys.Count > 1)
        {
            _logger.LogWarning("Multiple expected public keys existed in the order: {keys}", string.Join("; ", validationContext.ExpectedPublicKeys));
            return;
        }

        // Check if the expected public key matches the public key in the CSR
        var publicKey = certificateRequest.PublicKey.EncodedKeyValue.Format(false);
        var expectedPublicKey = validationContext.ExpectedPublicKeys.First();

        var isValid = string.Equals(publicKey, expectedPublicKey, StringComparison.Ordinal);
        _logger.LogInformation("Validated expectedPublicKey against certificate request. Result: {isValid}", isValid);

        if (isValid)
        {
            validationContext.SetPublicKeyUsed(expectedPublicKey);
        }
    }
}