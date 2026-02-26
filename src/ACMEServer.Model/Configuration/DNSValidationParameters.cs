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
        public string[]? AllowedDNSNames { get; set; } = default!;


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AllowedDNSNames == null)
            {
                yield return new ValidationResult("Allowed DNS names cannot be null, set [] if you want to disallow all names.", [nameof(AllowedDNSNames)]);
            }
        }
    }

    /// <summary>
    /// Parameters for IP identifier validation.
    /// </summary>
    public class IPValidationParameters : IValidatableObject
    {
        /// <summary>
        /// The IP networks that are allowed for this profile, e.g. 127.0.0.1/32 (CIDR notation)
        /// The default values are ::0/0 and 0.0.0.0/0, which means all IPv6 and IPv4 addresses are allowed.
        /// </summary>
        [NotNull]
        public string[]? AllowedIPNetworks { get; set; } = default!;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AllowedIPNetworks is not null)
            {
                foreach (var network in AllowedIPNetworks)
                {
                    if (!IPNetwork.TryParse(network, out _))
                    {
                        yield return new ValidationResult($"Invalid IP network format: {network}", [nameof(AllowedIPNetworks)]);
                    }
                }
            }
        }
    }
}
