namespace Th11s.ACMEServer.Configuration;

public class ACMEServerOptions
{
    // TODO: Background checking the status of orders and authorizations is probably not neccessary anymore - we should only do it upon startup to populate the queues.
    public BackgroundServiceOptions HostedWorkers { get; set; } = new ();

    /// <summary>
    /// Gets or sets the canonical host name.
    /// </summary>
    public string? CanonicalHostname { get; set; }

    /// <summary>
    /// The list of all caa identities the server accepts as it's own.
    /// </summary>
    public List<string> CAAIdentities { get; set; } = [];

    /// <summary>
    /// A website url to be included in the directory metadata.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Configuration for TOS. See <see cref="TermsOfServiceOptions"/>
    /// </summary>
    public TermsOfServiceOptions TOS { get; set; } = new ();

    /// <summary>
    /// Gets or sets options for external account binding used when creating or registering an account.
    /// </summary>
    /// <remarks>Configure this value only when the certificate authority requires external account
    /// binding.</remarks>
    public ExternalAccountBindingOptions? ExternalAccountBinding { get; set; }
    
    /// <summary>
    /// A feature switch deciding, if the server handles revokation. Also controls if the revokation endpoint is advertised in directory data.
    /// </summary>
    public bool SupportsRevokation { get; set; }

    /// <summary>
    /// Configures the background queues for challenge validation and certificate issuance. See <see cref="QueueConfiguration"/>
    /// </summary>
    public QueueConfiguration QueueSettings { get; set; } = new();

    /// <summary>
    /// Configures ACME Renewal Information (RFC 9773). See <see cref="ARIConfiguration"/>
    /// </summary>
    public ARIConfiguration ARI { get; set; } = new();

    /// <summary>
    /// Configures DNS-persist-01 pre provisioning informations. See <see cref="DnsPersist01Configuration"/>
    /// </summary>
    public DnsPersist01Configuration? DnsPersistPreProvisioning { get; set; }
}
