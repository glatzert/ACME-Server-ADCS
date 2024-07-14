using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TGIT.ACME.Storage.FileStore
{
    internal static class JsonDefaults
    {
        static JsonDefaults()
        {
            var settings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                NullValueHandling = NullValueHandling.Include,
            };

            settings.Converters.Add(new StringEnumConverter());
            Settings = settings;
        }

        public static readonly JsonSerializerSettings Settings;
    }
}
