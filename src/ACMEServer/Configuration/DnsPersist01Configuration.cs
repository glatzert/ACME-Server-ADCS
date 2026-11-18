namespace Th11s.ACMEServer.Configuration;

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
