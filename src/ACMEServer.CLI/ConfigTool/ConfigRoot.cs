using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Configuration;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.CLI.ConfigTool;

internal class ConfigRoot
{
    public ConfigRoot(ConfigArguments? args, IConfiguration? configuration)
    {
        // TODO: if configuration is not null, bind it.

        if (!string.IsNullOrWhiteSpace(args?.DnsHostName))
        {
            ServerOptions.SetCanonicalHostName(args.DnsHostName);
        }
    }

    public ACMEServerOptions ServerOptions { get; } = new();

    public FileStoreOptions FileStoreOptions { get; } = new();

    public DNSOverrideOptions DNSOverrideOptions { get; } = new();

    public List<ProfileConfiguration> Profiles { get; } = new();

    public Dictionary<string, object> BuildSerializableConfig()
    {
        var serverOptions = BuildSerializableServerOptions();
        var fileStoreOptions = BuildSerializableFileStoreOptions();

        var profiles = BuildSerializableProfileOptions();

        var result = new Dictionary<string, object>
        {
            { "AcmeServer", serverOptions },
            { "AcmeFileStore", fileStoreOptions },
            { "Profiles", profiles },
        };

        if (DNSOverrideOptions.NameServers.Count > 0)
        {
            result.Add("DNS", DNSOverrideOptions);
        }

        return result;
    }



    private Dictionary<string, object> BuildSerializableServerOptions()
    {
        var serverOptions = new Dictionary<string, object>
        {
            { nameof(ACMEServerOptions.CAAIdentities), ServerOptions.CAAIdentities },

            { nameof(ACMEServerOptions.SupportsRevokation), ServerOptions.SupportsRevokation }
        };

        if (!string.IsNullOrWhiteSpace(ServerOptions.WebsiteUrl))
        {
            serverOptions.Add(nameof(ACMEServerOptions.WebsiteUrl), ServerOptions.WebsiteUrl);
        }

        if (ServerOptions.TOS.RequireAgreement || !string.IsNullOrEmpty(ServerOptions.TOS.Url))
        {
            serverOptions.Add(nameof(ACMEServerOptions.TOS), ServerOptions.TOS);
        }

        if (ServerOptions.ExternalAccountBinding != null)
        {
            serverOptions.Add(nameof(ACMEServerOptions.ExternalAccountBinding), ServerOptions.ExternalAccountBinding);
        }

        return serverOptions;
    }
    private Dictionary<string, object> BuildSerializableFileStoreOptions()
    {
        return new Dictionary<string, object>
        {
            { nameof(FileStoreOptions.BasePath), FileStoreOptions.BasePath  },
        };
    }
    
    private Dictionary<string, object> BuildSerializableProfileOptions()
    {
        var profiles = new Dictionary<string, object>();

        foreach(var profileConfig in Profiles)
        {
            var profile = new Dictionary<string, object>
            {
                { nameof(ProfileConfiguration.SupportedIdentifiers), profileConfig.SupportedIdentifiers },
                { nameof(ProfileConfiguration.CertificateServices), profileConfig.CertificateServices },

                { nameof(ProfileConfiguration.RequireExternalAccountBinding), profileConfig.RequireExternalAccountBinding },
            };

            profiles.Add(profileConfig.Name, profile);
        }

        return profiles;
    }
}

internal class ConfigArguments
{
    public string? DnsHostName { get; set; }
}