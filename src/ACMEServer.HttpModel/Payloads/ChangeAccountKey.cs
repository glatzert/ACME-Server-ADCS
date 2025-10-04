using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.HttpModel.Payloads;

/// <summary>
/// Represents the payload for an ACME Change Account Key request.
/// https://www.rfc-editor.org/rfc/rfc8555#section-7.3.5
/// </summary>
public class ChangeAccountKey
{
    [JsonPropertyName("account")]
    public required string Account { get; set; } 

    [JsonPropertyName("oldKey")]
    public required string OldKey { get; set; }
}