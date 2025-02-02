using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.HttpModel.Requests.JWS;


public class AcmeJwsToken
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };


    [JsonPropertyName("protected")]
    public string Protected { get; }

    [JsonPropertyName("payload")]
    public string? Payload { get; }

    [JsonPropertyName("signature")]
    public string Signature { get; }


    [JsonIgnore]
    public AcmeJwsHeader AcmeHeader { get; }

    [JsonIgnore]
    public JsonDocument? AcmePayload { get; }

    [JsonIgnore]
    public byte[] SignatureBytes { get; }


    [JsonConstructor]
    public AcmeJwsToken(string @protected, string? payload, string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(@protected, nameof(@protected));
        ArgumentException.ThrowIfNullOrWhiteSpace(signature, nameof(signature));

        Protected = @protected;
        Payload = payload;
        Signature = signature;


        AcmeHeader = JsonSerializer.Deserialize<AcmeJwsHeader>(Base64UrlEncoder.Decode(Protected), _jsonOptions)
            ?? throw new InvalidOperationException("Header is null");

        AcmePayload = !string.IsNullOrWhiteSpace(Payload)
            ? JsonDocument.Parse(Base64UrlEncoder.Decode(Payload))
            : null;

        SignatureBytes = Base64UrlEncoder.DecodeBytes(Signature);
    }
}
