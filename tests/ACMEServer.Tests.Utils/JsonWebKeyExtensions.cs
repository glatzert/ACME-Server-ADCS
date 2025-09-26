using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text.Json;
using Th11s.ACMEServer.Model.JWS;

namespace ACMEServer.Tests.Utils
{
    public static class JsonWebKeyExtensions
    {
        public static string ExportPublicJwkJson(this JsonWebKey jsonWebKey)
        {
            return JsonSerializer.Serialize(jsonWebKey.ExportPublicJwk());
        }

        public static OrderedDictionary ExportPublicJwk(this JsonWebKey jsonWebKey)
        {
            if (jsonWebKey.Kty == "RSA")
            {
                return new OrderedDictionary
            {
                { "e", Base64UrlEncoder.Encode(jsonWebKey.E) },
                { "kty", jsonWebKey.Kty },
                { "n", Base64UrlEncoder.Encode(jsonWebKey.N) }
            };
            }

            throw new Exception("Unsupported key type");
        }

        public static Jwk ToAcmeJwk(this JsonWebKey jsonWebKey)
        {
            return new Jwk(jsonWebKey.ExportPublicJwkJson());
        }
    }

    public static class JsonWebKeyFactory
    {
        public static JsonWebKey CreateRsaJsonWebKey(int keySize = 2048)
        {
            var rsa = RSA.Create(keySize);
            var rsaSecurityKey = new RsaSecurityKey(rsa);
            return JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaSecurityKey);
        }
    }
}
