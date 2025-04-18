using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.HttpModel.Payloads;

/// <summary>
/// Represents the payload for an ACME Finalize Order request.
/// https://datatracker.ietf.org/doc/html/rfc8555#section-7.4
/// </summary>
public class FinalizeOrder
{
    [JsonPropertyName("csr")]
    public string? Csr { get; set; }
}
