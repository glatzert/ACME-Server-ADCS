using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Th11s.ACMEServer.Model.Configuration
{
    /// <summary>
    /// Parameters for DNS identifier validation.
    /// </summary>
    public class DNSValidationParameters : IValidatableObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether Certificate Authority Authorization (CAA) evaluation should be
        /// skipped during identifier validation.
        /// </summary>
        /// <remarks>Set this property to <see langword="true"/> to bypass CAA record checks.
        /// Skipping CAA evaluation may have security implications and should only be used
        /// in scenarios where CAA compliance is not required.</remarks>
        public bool SkipCAAEvaluation { get; set; }

        /// <summary>
        /// If true, the ACME server will reject wildcard DNS names during validation. The default is false.
        /// </summary>
        public bool DisableWildcards { get; set; }

        /// <summary>
        /// The DNS names that are allowed for this profile, e.g. "example.com"
        /// The values will be checked by using a case-insenstive, trimmed "EndsWith"
        /// </summary>
        [NotNull]
        public HashSet<string>? AllowedDNSNames { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AllowedDNSNames == null)
            {
                yield return new ValidationResult("Allowed DNS names cannot be null, set [] if you want to disallow all names.", [nameof(AllowedDNSNames)]);
            }
        }
    }
}
