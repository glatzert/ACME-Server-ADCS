namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// RenewalInfo Object as defined for ARI
/// https://www.rfc-editor.org/info/rfc9773/#section-4.2
/// </summary>
public class RenewalInfo
{
    /// <summary>
    /// The window of time in which the CA recommends renewing the certificate.
    /// </summary>
    public required RenewalPeriod SuggestedWindow { get; set; }

    /// <summary>
    /// A URL pointing to a page that may explain why the suggested renewal window has its current value.
    /// </summary>
    public string? ExplanationUrl { get; set; }
}

public class RenewalPeriod
{
    /// <summary>
    /// Start of the suggested renewal window
    /// </summary>
    public required string Start { get; set; }

    /// <summary>
    /// End of the suggested renewal window
    /// </summary>
    public required string End { get; set; }
}
