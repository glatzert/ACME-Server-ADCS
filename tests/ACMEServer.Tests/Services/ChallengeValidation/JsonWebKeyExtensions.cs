using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.Text.Json;

namespace ACMEServer.Tests.Services.ChallengeValidation
{
    internal static class JsonWebKeyExtensions
    {
        public static string ExportPublicJwkJson(this JsonWebKey jsonWebKey)
        {
            if (jsonWebKey.Kty == "RSA")
            {
                var dictionary = new OrderedDictionary
                {
                    { "e", Base64UrlEncoder.Encode(jsonWebKey.E) },
                    { "kty", jsonWebKey.Kty },
                    { "n", Base64UrlEncoder.Encode(jsonWebKey.N) }
                };

                return JsonSerializer.Serialize(dictionary);
            }

            throw new Exception("Unsupported key type");
        }
    }
}
