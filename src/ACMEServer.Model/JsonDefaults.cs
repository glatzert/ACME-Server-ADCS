using System.Text.Json;

namespace Th11s.ACMEServer.Model
{
    public static class JsonDefaults
    {
        public static JsonSerializerOptions JwsPayloadOptions { get; } = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }
}
