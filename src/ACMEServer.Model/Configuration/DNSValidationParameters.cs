using System.Net;

namespace Th11s.ACMEServer.Model.Configuration
{
    /// <summary>
    /// Parameters for DNS identifier validation.
    /// </summary>
    public class DNSValidationParameters
    {
        /// <summary>
        /// The DNS names that are allowed for this profile, e.g. "example.com"
        /// The values will be checked by using a case-insenstive, trimmed "EndsWith"
        /// </summary>
        public string[] AllowedDNSNames { get; set; } = [""];
    }

    /// <summary>
    /// Parameters for IP identifier validation.
    /// </summary>
    public class IPValidationParameters
    {
        /// <summary>
        /// The IP networks that are allowed for this profile, e.g. 127.0.0.1/32 (CIDR notation)
        /// The default values are ::0/0 and 0.0.0.0/0, which means all IPv6 and IPv4 addresses are allowed.
        /// </summary>
        public string[] AllowedIPNetworks { get; set; } = [ "::0/0", "0.0.0.0/0"];
    }
}
