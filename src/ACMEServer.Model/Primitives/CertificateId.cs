using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Th11s.ACMEServer.Model.Primitives;

[DebuggerDisplay("Certificate: {Value}")]
[DebuggerStepThrough]
public record class CertificateId : ResourceIdentifier
{
    public CertificateId() : base()
    { }

    public CertificateId(string value) : base(value)
    { }

    /// <summary>
    /// Builds the identifier from the given certificate as per ACME ARI specification.
    /// base64url(AKI keyIdentifier).base64url(Serial)
    /// </summary>
    public static CertificateId FromX509Certificate(X509Certificate2 certificate)
    {
        var authorityKeyIdentifier = certificate.Extensions.OfType<X509AuthorityKeyIdentifierExtension>()
            .FirstOrDefault()?.KeyIdentifier?.ToArray() ?? Encoding.ASCII.GetBytes("AKI not found");
        var serialNumber = certificate.SerialNumberBytes.ToArray();

        return new($"{Base64UrlEncoder.Encode(authorityKeyIdentifier)}.{Base64UrlEncoder.Encode(serialNumber)}");
    }
}

