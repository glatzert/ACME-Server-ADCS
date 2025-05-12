namespace Th11s.ACMEServer.Model.Configuration
{
    /// <summary>
    /// Parameters for DNS validation.
    /// </summary>
    public class DNSValidationParameters
    {
        /// <summary>
        /// The DNS names that are allowed for this profile, e.g. "example.com"
        /// The values will be checked by using a case-insenstive, trimmed "EndsWith"
        /// </summary>
        public string[] AllowedDNSNames { get; set; } = [""];
    }
}
