namespace Th11s.ACMEServer.HttpModel.Payloads;

public class RevokeCertificate
{
    public required string Certificate { get; set; }
    public int? Reason { get; set; }
}
