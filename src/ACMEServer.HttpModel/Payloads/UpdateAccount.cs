namespace Th11s.ACMEServer.HttpModel.Payloads;

public class UpdateAccount
{
    public string? Status { get; set; }
    public List<string>? Contact { get; set; }

    public bool? TermsOfServiceAgreed { get; set; }
}
