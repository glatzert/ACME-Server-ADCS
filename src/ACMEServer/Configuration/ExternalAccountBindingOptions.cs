namespace Th11s.ACMEServer.Configuration
{
    public class ExternalAccountBindingOptions
    {
        public bool Required { get; set; }
        
        public required string MACRetrievalUrl { get; set; }

        public string? SuccessSignalUrl { get; set; }
        public string? FailedSignalUrl { get; set; }

        public List<ExternalAccountBindingHeader> Headers { get; set; } = [];
    }

    public class ExternalAccountBindingHeader
    {
        public required string Key { get; set; }
        public required string Value { get; set; }
    }
}