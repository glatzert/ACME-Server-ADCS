using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.HttpModel.Payloads;

/// <summary>
/// Represents the payload for an ACME Create Order request.
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.4
/// </summary>
public class CreateOrder
{
    [JsonPropertyName("identifiers")]
    public List<Identifier>? Identifiers { get; set; }


    [JsonPropertyName("notBefore")]
    public DateTimeOffset? NotBefore { get; set; }

    [JsonPropertyName("notAfter")]
    public DateTimeOffset? NotAfter { get; set; }
}
