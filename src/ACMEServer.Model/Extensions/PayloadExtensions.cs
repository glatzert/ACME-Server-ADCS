using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace Th11s.ACMEServer.Model.Extensions
{
    public static class PayloadExtensions
    {
        public static T? DeserializeBase64UrlEncodedJson<T>(this string? encoded)
            where T : class
        {
            if(encoded is null)
                return null;

            var decoded = Base64UrlEncoder.Decode(encoded);
            if(decoded is null)
                return null;

            return JsonSerializer.Deserialize<T>(decoded, JsonDefaults.JwsPayloadOptions);
        }
    }
}
