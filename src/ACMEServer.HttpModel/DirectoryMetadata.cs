namespace TGIT.ACME.Protocol.HttpModel
{
    /// <summary>
    /// Describes the HTTP-Response-Model for ACME DirectoryMetadata
    /// https://tools.ietf.org/html/rfc8555#section-7.1.1
    /// </summary>
    public class DirectoryMetadata
    {
        public string? TermsOfService { get; set; }
        public string? Website { get; set; }
        public string? CAAIdentities { get; set; }
        public bool? ExternalAccountRequired { get; set; }
    }
}
