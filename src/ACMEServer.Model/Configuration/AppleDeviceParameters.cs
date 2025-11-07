using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class AppleDeviceParameters : IValidatableObject
    {
        public string[] RootCertificates { get; set; } = ["MIICJDCCAamgAwIBAgIUQsDCuyxyfFxeq/bxpm8frF15hzcwCgYIKoZIzj0EAwMwUTEtMCsGA1UEAwwkQXBwbGUgRW50ZXJwcmlzZSBBdHRlc3RhdGlvbiBSb290IENBMRMwEQYDVQQKDApBcHBsZSBJbmMuMQswCQYDVQQGEwJVUzAeFw0yMjAyMTYxOTAxMjRaFw00NzAyMjAwMDAwMDBaMFExLTArBgNVBAMMJEFwcGxlIEVudGVycHJpc2UgQXR0ZXN0YXRpb24gUm9vdCBDQTETMBEGA1UECgwKQXBwbGUgSW5jLjELMAkGA1UEBhMCVVMwdjAQBgcqhkjOPQIBBgUrgQQAIgNiAAT6Jigq+Ps9Q4CoT8t8q+UnOe2poT9nRaUfGhBTbgvqSGXPjVkbYlIWYO+1zPk2Sz9hQ5ozzmLrPmTBgEWRcHjA2/y77GEicps9wn2tj+G89l3INNDKETdxSPPIZpPj8VmjQjBAMA8GA1UdEwEB/wQFMAMBAf8wHQYDVR0OBBYEFPNqTQGd8muBpV5du+UIbVbi+d66MA4GA1UdDwEB/wQEAwIBBjAKBggqhkjOPQQDAwNpADBmAjEA1xpWmTLSpr1VH4f8Ypk8f3jMUKYz4QPG8mL58m9sX/b2+eXpTv2pH4RZgJjucnbcAjEA4ZSB6S45FlPuS/u4pTnzoz632rA+xW/TZwFEh9bhKjJ+5VQ9/Do1os0u3LEkgN/r"];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var cert in RootCertificates)
            {
                if (string.IsNullOrWhiteSpace(cert))
                {
                    yield return new ValidationResult($"Root certificates contained empty entries.", [nameof(RootCertificates)]);
                    continue;
                }

                bool isParsable;
                try
                {
#if NET10_0_OR_GREATER
                    using var x509 = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(cert));
#else
                    using var x509 = new X509Certificate2(Convert.FromBase64String(cert));
#endif
                    isParsable = true;
                }
                catch
                {
                    isParsable = false;
                }

                if (!isParsable)
                {
                    yield return new ValidationResult($"Root certificate '{cert}' was not parsable.", [nameof(RootCertificates)]);
                }
            }
        }
    }
}
