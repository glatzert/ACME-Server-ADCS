using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class DefaultPublicKeyAnalyzer(ILogger<DefaultPublicKeyAnalyzer> logger) : IPublicKeyAnalyzer
{
    private readonly ILogger<DefaultPublicKeyAnalyzer> _logger = logger;

    public async Task<PublicKeyInfo?> AnalyzePublicKeyAsync(string certificateSigningRequest, CancellationToken cancellationToken)
    {
        CertificateRequest certificateRequest;
        try
        {
            certificateRequest = CertificateRequest.LoadSigningRequest(
                Base64UrlTextEncoder.Decode(certificateSigningRequest!),
                HashAlgorithmName.SHA256 // we'll not sign the request, so this is more a placeholder than anything else
            );
        }
        catch (Exception ex)
        {
            _logger.CsrDecodeFailed(ex);
            return null;
        }

        if (certificateRequest.PublicKey.GetRSAPublicKey() is RSA rsaPublicKey)
        {
            return new PublicKeyInfo("RSA", rsaPublicKey.KeySize);
        }
        else if (certificateRequest.PublicKey.GetECDsaPublicKey() is ECDsa ecdsaPublicKey)
        {
            return new PublicKeyInfo("ECDsa", ecdsaPublicKey.KeySize);
        }
        else if (certificateRequest.PublicKey.GetECDiffieHellmanPublicKey() is ECDiffieHellman ecdhPublicKey)
        {
            return new PublicKeyInfo("ECDH", ecdhPublicKey.KeySize);
        }
        else
        {
            // TODO: _logger.UnsupportedPublicKeyAlgorithm(certificateRequest.PublicKey);
        }

        return null;
    }
}
