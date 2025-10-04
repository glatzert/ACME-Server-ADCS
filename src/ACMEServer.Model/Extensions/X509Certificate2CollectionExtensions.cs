using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Model.Extensions
{
    public static class X509Certificate2CollectionExtensions
    {
        public static X509Certificate2 GetLeafCertificate(this X509Certificate2Collection x509CertificateCollection)
        {
            if (x509CertificateCollection.Count == 0)
            {
                throw new InvalidOperationException("The X509Certificate2Collection is empty.");
            }

            if (x509CertificateCollection.Count == 1)
            {
                return x509CertificateCollection[0];
            }

            var leafCertificates = x509CertificateCollection.Where(
                x => x.Extensions.OfType<X509BasicConstraintsExtension>()
                    .Any(ext => !ext.CertificateAuthority)
            ).ToList();

            if (leafCertificates.Count > 1)
            {
                throw new InvalidOperationException("Multiple leaf certificates found in the X509Certificate2Collection.");
            }

            if (leafCertificates.Count == 0)
            {
                throw new InvalidOperationException("No leaf certificate found in the X509Certificate2Collection.");
            }

            return leafCertificates[0];
        }
    }
}
