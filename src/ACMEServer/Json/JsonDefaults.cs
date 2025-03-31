using System.Text.Json;
using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.Json
{
    public static class AcmeJsonDefaults
    {
        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; } = new JsonSerializerOptions().ApplyDefaultJsonSerializerOptions();

        public static JsonSerializerOptions ApplyDefaultJsonSerializerOptions(this JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            return options;
        }
    }
}
