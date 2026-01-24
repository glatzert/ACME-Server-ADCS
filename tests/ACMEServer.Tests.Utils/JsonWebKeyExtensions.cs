using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text.Json;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Tests.Utils
{
    internal static class JsonWebKeyExtensions
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
            else if(jsonWebKey.Kty == "EC")
            {
                return new OrderedDictionary
                {
                    { "crv", jsonWebKey.Crv },
                    { "kty", jsonWebKey.Kty },
                    { "x", Base64UrlEncoder.Encode(jsonWebKey.X) },
                    { "y", Base64UrlEncoder.Encode(jsonWebKey.Y) }
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

        public static JsonWebKey CreateECDsaJsonWebKey()
        {
            var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var ecdsaSecurityKey = new ECDsaSecurityKey(ecdsa);
            return JsonWebKeyConverter.ConvertFromECDsaSecurityKey(ecdsaSecurityKey);
        }
    }
}
