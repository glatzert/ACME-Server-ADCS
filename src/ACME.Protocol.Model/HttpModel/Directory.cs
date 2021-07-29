using System;

namespace TGIT.ACME.Protocol.HttpModel
{
    /// <summary>
    /// Describes the HTTP-Response-Model for an ACME Directory
    /// https://tools.ietf.org/html/rfc8555#section-7.1.1
    /// </summary>
    public class Directory
    {
        public string? NewNonce { get; set; }
        public string? NewAccount { get; set; }
        public string? NewOrder { get; set; }
        public string? NewAuthz { get; set; }
        public string? RevokeCert { get; set; }
        public string? KeyChange { get; set; }

        public DirectoryMetadata? Meta { get; set; }
    }
}
