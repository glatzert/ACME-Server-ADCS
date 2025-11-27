using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class AppleDeviceParameters : IValidatableObject
    {
        [NotNull]
        public string[]? RootCertificates { get; set; } = default!;

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
