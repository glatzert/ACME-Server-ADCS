using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Th11s.ACMEServer.Model.Configuration
{
    /// <summary>
    /// Parameters for DNS identifier validation.
    /// </summary>
    public class DNSValidationParameters : IValidatableObject
    {
        /// <summary>
        /// The DNS names that are allowed for this profile, e.g. "example.com"
        /// The values will be checked by using a case-insenstive, trimmed "EndsWith"
        /// </summary>
        public string[] AllowedDNSNames { get; set; } = [""];


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
        public string[] AllowedIPNetworks { get; set; } = [ "::0/0", "0.0.0.0/0"];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var network in AllowedIPNetworks)
            {
                if(!IPNetwork.TryParse(network, out _))
                {
                    yield return new ValidationResult($"Invalid IP network format: {network}", [nameof(AllowedIPNetworks)]);
                }
            }
        }
    }
}
