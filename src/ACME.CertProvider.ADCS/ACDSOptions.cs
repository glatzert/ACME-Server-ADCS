namespace TGIT.ACME.Protocol.IssuanceServices.ADCS
{
    public class ADCSOptions
    {
        public string CAServer { get; set; }
        public string? TemplateName { get; set; }

        public bool AllowEmptyCN { get; set; }
        public bool AllowCNSuffix { get; set; }
    }
}
