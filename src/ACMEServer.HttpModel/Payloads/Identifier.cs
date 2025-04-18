using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.HttpModel.Payloads;

/// <summary>
/// Defines an identifier as used in orders or authorizations
/// </summary>
public class Identifier
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
