namespace Th11s.ACMEServer.Configuration
{
    public class ACMEServerOptions
    {
        public BackgroundServiceOptions HostedWorkers { get; set; } = new ();

        public string? WebsiteUrl { get; set; }

        public TermsOfServiceOptions TOS { get; set; } = new ();

        public ExternalAccountBindingOptions? ExternalAccountBinding { get; set; }
    }
}
