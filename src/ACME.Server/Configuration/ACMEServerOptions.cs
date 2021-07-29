namespace TGIT.ACME.Server.Configuration
{
    public class ACMEServerOptions
    {
        public BackgroundServiceOptions HostedWorkers { get; set; } = new BackgroundServiceOptions();

        public string? WebsiteUrl { get; set; }

        public TOSOptions TOS { get; set; } = new TOSOptions();
    }
}
