using ACMEServer.Storage.FileSystem.Configuration;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.ConfigCLI;

internal class ConfigRoot
{
    [JsonPropertyName("AcmeServer")]
    public ACMEServerOptions ServerOptions { get; } = new();

    [JsonPropertyName("AcmeFileStore")]
    public FileStoreOptions FileStoreOptions { get; } = new();

    [JsonPropertyName("DNS")]
    public DNSOverrideOptions DNSOverrideOptions { get; } = new();

    [JsonPropertyName("Profiles")]
    public ProfileOptions Profiles { get; } = new();

    internal class ProfileOptions
    {
        internal Dictionary<string, ProfileConfiguration> Items { get; set; } = [];
    }
}
