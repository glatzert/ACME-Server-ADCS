namespace Th11s.ACMEServer.Configuration;

public class ACMEServerOptions
{
    // TODO: Background checking the status of orders and authorizations is probably not neccessary anymore - we should only do it upon startup to populate the queues.
    public BackgroundServiceOptions HostedWorkers { get; set; } = new ();

    public string? CanonicalHostname { get; set; }

    public List<string> CAAIdentities { get; set; } = [];

    
    public string? WebsiteUrl { get; set; }

    public TermsOfServiceOptions TOS { get; set; } = new ();

    public ExternalAccountBindingOptions? ExternalAccountBinding { get; set; }
    
    public bool SupportsRevokation { get; set; }

    public QueueConfiguration QueueSettings { get; set; } = new();

    public DnsPersist01Configuration? DnsPersistPreProvisioning { get; set; }
}

public class QueueConfiguration
{
    public int SyncValidationTimeout { get; set; } = 500;
    public int SyncIssuanceTimeout { get; set; } = 500;
}

public class  DnsPersist01Configuration
{
    /// <summary>
    /// Support for dns-persist-01 pre-provision
    /// https://datatracker.ietf.org/doc/draft-ietf-acme-dns-persist/ (Section 3.2)
    /// </summary>
    public List<string>? IssuerDomainNames { get; set; }

    /// <summary>
    /// Support for dns-persist-01 pre-provision
    /// https://datatracker.ietf.org/doc/draft-ietf-acme-dns-persist/ (Section 3.2)
    /// </summary>
    public string? AccountHashPrefix { get; set; }
}
