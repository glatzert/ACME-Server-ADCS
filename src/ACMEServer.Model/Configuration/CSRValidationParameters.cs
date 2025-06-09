namespace Th11s.ACMEServer.Model.Configuration
{
    public class CSRValidationParameters
    {
        public CSRSANParameters AllowedSANValues { get; set; } = new ();
    }

    public class CSRSANParameters
    {
        public string[] DNSNames { get; set; } = [];
        public string[] IPAddresses { get; set; } = [];
    }
}