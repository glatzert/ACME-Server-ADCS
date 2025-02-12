namespace Th11s.ACMEServer.Configuration
{
    public class ExternalAccountBindingOptions
    {
        public bool Required { get; set; }
        
        public required string MACRetrievalUrl { get; set; }

        public string? SuccessSignalUrl { get; set; }
        public string? FailedSignalUrl { get; set; }
    }
}