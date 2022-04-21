namespace TGIT.ACME.Protocol.IssuanceServices.ACDS
{
    public class ACDSOptions
    {
        public string CAServer { get; set; }
        public string? TemplateName { get; set; }

        public bool AllowEmptyCN { get; set; }
        public bool AllowCNSuffix { get; set; }
    }
}
