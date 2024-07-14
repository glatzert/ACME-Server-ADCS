namespace TGIT.ACME.Protocol.HttpModel.Requests
{
    /// <summary>
    /// Defines an identifier as used in orders or authorizations
    /// </summary>
    public class Identifier
    {
        public string? Type { get; set; }
        public string? Value { get; set; }
    }
}
