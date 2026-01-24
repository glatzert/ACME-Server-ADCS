using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.Tests.Utils;

public static class HttpRequestMessageExtensions
{
    private static JsonSerializerOptions _jsonSerializerOptions = new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static void CreateAcmeMessage<T>(
        this HttpRequestMessage request, 
        JsonWebKey jwk, 
        string nonce, 
        string? kid,
        T payloadObject,
        
        Dictionary<string, object?> overrides)
    {
        var jwsHeaderData = new Dictionary<string, object?>
        {
            { "alg", overrides.TryGetValue("alg", out var algOverride) ? algOverride : jwk.Kty == "EC" ? "ES256" : "RS256" },
            { "url", overrides.TryGetValue("url", out var urlOverride) ? urlOverride : request.RequestUri!.AbsoluteUri },
            { "nonce", overrides.TryGetValue("nonce", out var nonceOverride) ? nonceOverride : nonce },
        };

        if (kid == null)
        {
            jwsHeaderData["jwk"] = jwk.ExportPublicJwk();
        }
        else
        {
            jwsHeaderData["kid"] = jwk.Kid;
        }

        if (overrides.TryGetValue("jwk", out var jwkOverride))
        {
            jwsHeaderData["jwk"] = jwkOverride;
        }
        if (overrides.TryGetValue("kid", out var kidOverride))
        {
            jwsHeaderData["kid"] = kidOverride;
        }

        var jwsHeader = JsonSerializer.Serialize(jwsHeaderData, _jsonSerializerOptions);
        var jwsPayload = JsonSerializer.Serialize(payloadObject, _jsonSerializerOptions);

        var jwsRequest = new Dictionary<string, object?>
        {
            { "protected", Base64UrlEncoder.Encode(jwsHeader) },
            { "payload", Base64UrlEncoder.Encode(jwsPayload) },
        };


        if (overrides.TryGetValue("signature", out var value))
        {
            jwsRequest["signature"] = value;
        }
        else
        {
            using var signatureProvider = new AsymmetricSignatureProvider(jwk, SecurityAlgorithms.RsaSha256, true);
            var signature = signatureProvider.Sign(Encoding.UTF8.GetBytes($"{jwsRequest["protected"]}.{jwsRequest["payload"]}"));
            jwsRequest["signature"] = Base64UrlEncoder.Encode(signature);
        }

        var requestBody = JsonSerializer.Serialize(jwsRequest, _jsonSerializerOptions);

        request.Content = new StringContent(
            requestBody,
            Encoding.UTF8, 
            "application/jose+json");
    }
}
