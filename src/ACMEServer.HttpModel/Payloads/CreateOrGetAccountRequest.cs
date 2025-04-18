using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.HttpModel.Payloads;

public class CreateOrGetAccount
{
    public List<string>? Contact { get; set; }

    public bool TermsOfServiceAgreed { get; set; }
    public bool OnlyReturnExisting { get; set; }

    public AcmeJwsToken? ExternalAccountBinding { get; set; }
}
