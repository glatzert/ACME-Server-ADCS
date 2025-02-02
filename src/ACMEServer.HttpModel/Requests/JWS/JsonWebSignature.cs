using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.HttpModel.Requests.JWS;


public class JsonWebSignature
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };


    public RawJWSData RawData { get; }

    public JOSEHeader Header { get; }
    public JsonDocument? Payload { get; }
    public byte[] Signature { get; }


    [JsonConstructor]
    public JsonWebSignature(string @protected, string? payload, string signature)
    {
        RawData = new (Header: @protected, Payload: payload, Signature: signature);

        Header = JsonSerializer.Deserialize<JOSEHeader>(Base64UrlEncoder.Decode(RawData.Header), _jsonOptions)
            ?? throw new InvalidOperationException("Header is null");

        Payload = !string.IsNullOrWhiteSpace(RawData.Payload)
            ? JsonDocument.Parse(Base64UrlEncoder.Decode(RawData.Payload))
            : null;

        Signature = Base64UrlEncoder.DecodeBytes(RawData.Signature);
    }



    public record RawJWSData(string Header, string? Payload, string Signature);
}
