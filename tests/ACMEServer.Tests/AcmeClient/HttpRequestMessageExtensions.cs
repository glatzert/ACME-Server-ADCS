using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Th11s.AcmeServer.Tests.AcmeClient
{
    public static class HttpRequestMessageExtensions
    {
        public static void CreateAcmeMessage<T>(
            this HttpRequestMessage request, 
            JsonWebKey jwk, 
            string nonce, 
            string? kid,
            T payloadObject,
            
            Dictionary<string, object> overrides)
        {
            var jwsHeaderData = new Dictionary<string, object>
            {
                { "alg", overrides.TryGetValue("alg", out var algOverride) ? algOverride : "RS256" },
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

            var jwsHeader = JsonSerializer.Serialize(jwsHeaderData);


            var jwsPayload = JsonSerializer.Serialize(payloadObject, 
                new JsonSerializerOptions() {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false,
                });


            var jwsRequest = new Dictionary<string, object>
            {
                { "protected", Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(jwsHeader)) },
                { "payload", Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(jwsPayload)) },
            };


            if(overrides.ContainsKey("signature"))
            {
                jwsRequest["signature"] = overrides["signature"];
            }
            else
            {
                using var signaturProvider = new AsymmetricSignatureProvider(jwk, SecurityAlgorithms.RsaSha256);
                jwsRequest["signature"] = Base64UrlEncoder.Encode(signaturProvider.Sign(Encoding.UTF8.GetBytes($"{jwsRequest["protected"]}.{jwsRequest["payload"]}")));
            }


            request.Content = new StringContent(
                JsonSerializer.Serialize(jwsRequest),
                Encoding.UTF8, 
                "application/jose+json");
        }
    }
}
