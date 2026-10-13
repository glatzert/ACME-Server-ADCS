namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Describes the HTTP-Response-Model for ACME DirectoryMetadata
/// https://tools.ietf.org/html/rfc8555#section-7.1.1
/// </summary>
public class DirectoryMetadata
{
    public string[]? CAAIdentities { get; set; }

    public string? TermsOfService { get; set; }
    public string? Website { get; set; }
    public required bool ExternalAccountRequired { get; set; }

    /// <summary>
    /// Support for ACME Profiles, as defined in the ACME Profiles draft.
    /// https://datatracker.ietf.org/doc/draft-ietf-acme-profiles/ (Section 3)
    /// </summary>
    public required Dictionary<string, string> Profiles { get; set; }

    /// <summary>
    /// Support for dns-persist-01 pre-provisioning
    /// https://datatracker.ietf.org/doc/draft-ietf-acme-dns-persist/ (Section 3.2)
    /// </summary>
    public string[]? IssuerDomainNames { get; set; }

    /// <summary>
    /// Support for dns-persist-01 pre-provisioning
    /// (guessed, since it's not explicitly defined in the draft)
    /// </summary>
    public string? AccountHashPrefix { get; set; }
}


/// <summary>
/// A minimal output describing a profile configuration.
/// </summary>
public class ProfileMetadata
{
    public required string ProfileName { get; set; }

    public required bool ExternalAccountRequired { get; set; }

    public required string[] SupportedIdentifierTypes { get; set; }
    public required int Priority { get; set; }
    public required Dictionary<string, string[]> ChallengeTypes { get; set; }
}