namespace Th11s.ACMEServer.Model.Configuration
{
    public class CSRValidationParameters
    {
        public CSRSANParameters AllowedSANValues { get; set; } = new ();
    }

    public class CSRSANParameters
    {
        public string? DNSNameRegex { get; set; }

        public string[] IPNetworks { get; set; } = [];

        public string? URIRegex { get; set; }
    }
}