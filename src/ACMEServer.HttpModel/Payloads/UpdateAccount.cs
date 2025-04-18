using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.HttpModel.Payloads;

/// <summary>
/// Represents the payload for an ACME Update Account request.
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.3.2
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.3.3
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.3.6
/// </summary>
public class UpdateAccount
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("contact")]
    public List<string>? Contact { get; set; }


    [JsonPropertyName("termsOfServiceAgreed")]
    public bool? TermsOfServiceAgreed { get; set; }
}
