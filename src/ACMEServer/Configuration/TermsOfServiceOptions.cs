namespace Th11s.ACMEServer.Configuration;

public class TermsOfServiceOptions
{
    public bool RequireAgreement { get; set; }
    public string? Url { get; set; }

    public DateTime? LastUpdate { get; set; }
}
