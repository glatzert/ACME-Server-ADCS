using Microsoft.IdentityModel.Tokens;
using System.Collections.Specialized;
using System.Text.Json;

namespace Th11s.AcmeServer.Tests.AcmeClient;

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

        throw new Exception("Unsupported key type");
    }
}
