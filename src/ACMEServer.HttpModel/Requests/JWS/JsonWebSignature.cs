using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Th11s.ACMEServer.HttpModel.Requests.JWS;


public class JsonWebSignature
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public static JsonWebSignature FromEncodedForm(EncodedJsonWebSignature encodedJWS)
    {
        var headerJson = Base64UrlEncoder.Decode(encodedJWS.Header);
        var header = JsonSerializer.Deserialize<JOSEHeader>(headerJson, _jsonOptions)
            ?? throw new InvalidOperationException("Header is null");

        var payload = !string.IsNullOrWhiteSpace(encodedJWS.Payload)
            ? JsonDocument.Parse(Base64UrlEncoder.Decode(encodedJWS.Payload))
            : null;

        var signature = Base64UrlEncoder.DecodeBytes(encodedJWS.Signature);

        return new JsonWebSignature(encodedJWS, header, payload, signature);
    }


    [SetsRequiredMembers]
    public JsonWebSignature(EncodedJsonWebSignature encodedJWS, JOSEHeader header, JsonDocument? payload, byte[] signature)
    {
        EncodedJWS = encodedJWS;

        Header = header;
        Payload = payload;
        Signature = signature;
    }


    public EncodedJsonWebSignature EncodedJWS { get; }

    public JOSEHeader Header { get; }
    public JsonDocument? Payload { get; }

    public byte[] Signature { get; }
}
