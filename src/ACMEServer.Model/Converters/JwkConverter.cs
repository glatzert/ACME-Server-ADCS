using System.Text.Json;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.HttpModel.Converters
{
    public class JwkConverter : JsonConverter<Jwk>
    {
        public override Jwk Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jwkJson = JsonSerializer.Deserialize<object>(ref reader).ToString();
            return new Jwk(jwkJson);
        }

        public override void Write(Utf8JsonWriter writer, Jwk value, JsonSerializerOptions options)
        {
            throw new InvalidOperationException();
        }
    }
}
