using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.Model.JWS;

public class AcmeJwsToken
{
    [JsonPropertyName("protected")]
    public string Protected { get; }

    [JsonPropertyName("payload")]
    public string? Payload { get; }

    [JsonPropertyName("signature")]
    public string Signature { get; }


    [JsonIgnore]
    public AcmeJwsHeader AcmeHeader { get; }

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


        AcmeHeader = JsonSerializer.Deserialize<AcmeJwsHeader>(Base64UrlEncoder.Decode(Protected), JsonDefaults.JwsPayloadOptions)
            ?? throw new InvalidOperationException("Header is null");

        SignatureBytes = Base64UrlEncoder.DecodeBytes(Signature);
    }


    public bool TryGetPayload<T>(out T? payload)
    {
        if (Payload is null)
        {
            payload = default;
            return false;
        }

        try
        {
            payload = JsonSerializer.Deserialize<T>(Base64UrlEncoder.Decode(Payload), JsonDefaults.JwsPayloadOptions);
            return true;
        }
        catch (JsonException)
        {
            payload = default;
            return false;
        }
    }
}
