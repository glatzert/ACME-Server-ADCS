namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Describes the HTTP-Response-Model for an ACME Directory
/// https://www.rfc-editor.org/info/rfc8555/#section-7.1.1
/// </summary>
public class Directory
{
    public string? NewNonce { get; set; }
    public string? NewAccount { get; set; }
    public string? NewOrder { get; set; }
    public string? NewAuthz { get; set; }
    public string? RevokeCert { get; set; }
    public string? KeyChange { get; set; }

    /// <summary>
    /// Support for ARI (RFC 9773)
    /// https://www.rfc-editor.org/info/rfc9773/#section-3
    /// </summary>
    public string? RenewalInfo { get; set; }

    public DirectoryMetadata? Meta { get; set; }
}
