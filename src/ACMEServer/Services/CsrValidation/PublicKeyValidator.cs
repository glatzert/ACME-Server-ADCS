
namespace Th11s.ACMEServer.Services.CertificateSigningRequest;

internal class ExpectedPublicKeyValidator
{
    public ExpectedPublicKeyValidator()
    {
    }

    internal bool IsValid(CsrValidationContext validationContext)
    {
        // Check if there are expected public keys, if not we can skip the validation
        if (validationContext.ExpectedPublicKeys.Length == 0)
        {
            return true;
        }

        // Check if there are more than one expected public keys, if so we cannot validate
        if (validationContext.ExpectedPublicKeys.Length > 1)
        {
            return false;
        }

        // Check if the expected public key matches the public key in the CSR
        var publicKey = validationContext.CertificateRequest.PublicKey.EncodedKeyValue.Format(false);
        var expectedPublicKey = validationContext.ExpectedPublicKeys[0];

        return string.Equals(publicKey, expectedPublicKey, StringComparison.Ordinal);
    }
}