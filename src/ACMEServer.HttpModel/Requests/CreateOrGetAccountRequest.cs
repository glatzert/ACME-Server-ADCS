namespace Th11s.ACMEServer.HttpModel.Requests
{
    public class CreateOrGetAccount
    {
        public List<string>? Contact { get; set; }

        public bool TermsOfServiceAgreed { get; set; }
        public bool OnlyReturnExisting { get; set; }

        public string? ExternalAccountBinding { get; set; }
    }
}
