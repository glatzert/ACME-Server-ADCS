using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Th11s.ACMEServer.Model.Configuration
{
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
        public HashSet<string>? AllowedIPNetworks { get; set; }

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
