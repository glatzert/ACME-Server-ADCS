namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Describes the HTTP-Response-Model for ACME DirectoryMetadata
/// https://tools.ietf.org/html/rfc8555#section-7.1.1
/// </summary>
public class DirectoryMetadata
{
    public required string? TermsOfService { get; set; }
    public required string? Website { get; set; }
    public required string? CAAIdentities { get; set; }
    public required bool ExternalAccountRequired { get; set; }

    public required Dictionary<string, string?> Profiles { get; set; }
}


/// <summary>
/// A minimal output describing a profile configuration.
/// </summary>
public class ProfileMetadata
{
    public required string ProfileName { get; set; }

    public required bool ExternalAccountRequired { get; set; }

    public required string[] SupportedIdentifierTypes { get; set; }

}