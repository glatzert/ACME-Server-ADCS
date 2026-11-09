using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class AppleDeviceParameters : IValidatableObject
    {
        [NotNull]
        public HashSet<string>? RootCertificates { get; set; }

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
                    using var x509 = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(cert));
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
