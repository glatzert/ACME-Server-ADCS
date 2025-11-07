using System.ComponentModel.DataAnnotations;

namespace Th11s.ACMEServer.Configuration;

public class ACMEServerOptions
{
    // TODO: Background checking the status of orders and authorizations is probably not neccessary anymore - we should only do it upon startup to populate the queues.
    public BackgroundServiceOptions HostedWorkers { get; set; } = new ();

    public string[] CAAIdentities { get; set; } = [];

    public string? WebsiteUrl { get; set; }

    public TermsOfServiceOptions TOS { get; set; } = new ();

    public ExternalAccountBindingOptions? ExternalAccountBinding { get; set; }
    
    public bool SupportsRevokation { get; set; }
}
