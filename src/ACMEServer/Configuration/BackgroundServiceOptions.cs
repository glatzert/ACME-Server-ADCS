namespace Th11s.ACMEServer.Configuration
{
    public class BackgroundServiceOptions
    {
        public int ValidationCheckInterval { get; set; } = 60;
        public int IssuanceCheckInterval { get; set; } = 60;
    }
}
