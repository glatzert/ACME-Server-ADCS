using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Model.Extensions
{
    public static class X509Certificate2CollectionExtensions
    {
        public static X509Certificate2 GetLeafCertificate(this X509Certificate2Collection x509CertificateCollection)
            => x509CertificateCollection.Where(
                    x => x.Extensions.OfType<X509BasicConstraintsExtension>()
                        .Any(ext => !ext.CertificateAuthority)
                )
                .Single();
        }
    }
}
