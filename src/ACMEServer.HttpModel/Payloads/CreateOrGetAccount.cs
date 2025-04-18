using System.Text.Json.Serialization;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.HttpModel.Payloads;

/// <summary>
/// Represents the payload for an ACME Create or Get Account request.
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.3
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.3.1
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.3.4
/// </summary>
public class CreateOrGetAccount
{
    [JsonPropertyName("contact")]
    public List<string>? Contact { get; set; }

    [JsonPropertyName("termsOfServiceAgreed")]
    public bool TermsOfServiceAgreed { get; set; }
    
    [JsonPropertyName("onlyReturnExisting")]
    public bool OnlyReturnExisting { get; set; }


    [JsonPropertyName("externalAccountBinding")]
    public AcmeJwsToken? ExternalAccountBinding { get; set; }
}
