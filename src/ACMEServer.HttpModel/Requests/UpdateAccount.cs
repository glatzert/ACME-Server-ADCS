namespace TGIT.ACME.Protocol.HttpModel.Requests
{
    public class UpdateAccount
    {
        public string? Status { get; set; }
        public List<string>? Contact { get; set; }

        public bool? TermsOfServiceAgreed { get; set; }
    }
}
