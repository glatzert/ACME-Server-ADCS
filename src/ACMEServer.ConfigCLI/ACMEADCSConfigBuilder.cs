using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.ConfigCLI;

internal class MainMenuScreen(ConfigCLI parent, ConfigRoot configBuilder)
    : CLIScreen(parent)
{
    private readonly ConfigRoot _configRoot = configBuilder;

    protected override string? ScreenTitle => "ACME Server Configuration Builder";
    protected override string? ScreenDescription => "Welcome to the ACME Server Configuration Builder. Please select an option to configure different aspects of the server.";

    protected override List<CLIAction> Actions =>
    [
        new CLIAction('A', "Configure Server Settings", () => Parent.PushScreen(new ServerConfigScreen(Parent, _configRoot.ServerOptions))),
        new CLIAction('S', "Configure Storage", () => Parent.PushScreen(new StorageConfigScreen(Parent, _configRoot.FileStoreOptions))),
        new CLIAction('P', "Configure Profiles", () => Parent.PushScreen(new ProfilesConfigScreen(Parent, _configRoot.Profiles))),
        new CLIAction('D', "Configure DNS Overrides", () => Parent.PushScreen(new DNSConfigScreen(Parent, _configRoot.DNSOverrideOptions))),

        new CLIAction('Q', "Quit and Save Configuration", Parent.PopScreen)
    ];

    protected override List<ConfigInfo> GetConfigInfo()
        => [
            new(
                "Server configuration",
                "",
                Status.None
            ) {
                SubInfo = [
                    new(
                        "CAA Identities",
                        _configRoot.ServerOptions.CAAIdentities.JoinOr("none"),
                        _configRoot.ServerOptions.CAAStatus
                    ),

                    new(
                        "Revokation support enabled",
                        $"{_configRoot.ServerOptions.SupportsRevokation}",
                        Status.None
                    ),

                    new(
                        "Terms of service",
                        _configRoot.ServerOptions.TOS.RequireAgreement ? "agreement required" : "not configured",
                        Status.None
                    ),
                    new(
                        "External account binding",
                        _configRoot.ServerOptions.ExternalAccountBinding is null ? "disabled" : "enabled",
                        Status.None
                    )
                ]
            },
            new(
                "Storage",
                _configRoot.FileStoreOptions.BasePath ?? "n/a",
                _configRoot.FileStoreOptions.Status
            ),
            new(
                "Profiles",
                _configRoot.Profiles.Items.Select(x => x.Key).JoinOr(),
                _configRoot.Profiles.Status
            ),
            new(
                "DNS",
                _configRoot.DNSOverrideOptions.NameServers.Length > 0 ? "overriden" : "system default",
                Status.None
            )
        ];
}

internal class ProfilesConfigScreen(ConfigCLI parent, ConfigRoot.ProfileOptions options) : CLIScreen(parent)
{
    private readonly ConfigRoot.ProfileOptions _options = options;
    private ProfileConfiguration? _currentProfile;

    protected override string? ScreenTitle => "Profiles Configuration";
    protected override string? ScreenDescription => "Configure the profiles below.";
    protected override List<CLIAction> Actions
    {
        get
        {
            List<CLIAction> result = [];

            if (_currentProfile is null)
            {
                var profileList = _options.Items.Values.ToList();

                result.Add(new(
                    '+',
                    "Add new profile",
                    AddProfile,
                    () => profileList.Count > 0 ? Status.None : Status.NeedsAttention
                ));

                
                for (int i = 0; i < profileList.Count; i++)
                {
                    int idx = i; // capture for lambda
                    result.Add(
                        new CLIAction(
                            (char)('1' + i),
                            $"Edit profile '{profileList[i].Name}'",
                            () => _currentProfile = profileList[idx]
                        )
                    );
                }

                result.Add(
                    new('B', "Back to Main Menu", Parent.PopScreen)
                );
            }
            else
            {
                result.AddRange([
                    new (
                        'A',
                        "Select ADCS server and template",
                        ModifiyADCSOptions,
                        () => _currentProfile.ADCSOptions.Status
                    ),

                    new (
                        'S',
                        "Select supported identifiers",
                        SelectIdentifiers,
                        () => _currentProfile.SupportedIdentifiers.Length > 0 ? Status.AllGood : Status.NeedsAttention
                    ),

                    new (
                        'X',
                        "Remove current profile",
                        RemoveProfile
                    )
                ]);

                result.Add(
                    new('B', "Back to Profile selection", () => _currentProfile = null)
                );
            }

            return result;
        }
    }

    private void RemoveProfile()
    {
        throw new NotImplementedException();
    }

    private void AddProfile()
    {
        var newProfileName = CLIPrompt.String("Set profile name");
        if(string.IsNullOrWhiteSpace(newProfileName))
        {
            return;
        }

        _options.Items.Add(
            newProfileName,
            new()
            {
                ADCSOptions = new()
                {
                    CAServer = "",
                    TemplateName = ""
                },

                SupportedIdentifiers = ["dns"],
            }
        );

        _currentProfile = _options.Items[newProfileName];
    }

    private void SelectIdentifiers()
    {
        if(_currentProfile is null)
        {
            return;
        }

        var supportedIdentifiers = CLIPrompt.MultiSelect(
            "Choose supported identifiers",
            [IdentifierTypes.DNS, IdentifierTypes.IP, IdentifierTypes.PermanentIdentifier, IdentifierTypes.HardwareModule],
            i => i
        );

        _currentProfile.SupportedIdentifiers = [.. supportedIdentifiers];
    }

    private void ModifiyADCSOptions()
    {
        if (_currentProfile is null)
        {
            return;
        }

        var templates = ActiveDirectoryUtility.GetEnrollmentServiceCollection()
            .SelectMany(ca => ca.CertificateTemplates.Select(t => (CA: ca, Template: t)))
            .ToList();

        var selection = CLIPrompt.Select("Choose CA and Template", templates, t => $"{t.CA.Name} - {t.Template}");
        if (selection.CA is null)
        {
            return;
        }

        _currentProfile.ADCSOptions.CAServer = selection.CA.ConfigurationString;
        _currentProfile.ADCSOptions.TemplateName = selection.Template;
    }

    protected override List<ConfigInfo> GetConfigInfo()
    {
        if (_currentProfile is null)
        {
            return [];
        }
        else
        {
            return [
                new(
                    "ADCS Configuration",
                    $"{_currentProfile.ADCSOptions.CAServer} - {_currentProfile.ADCSOptions.TemplateName}",
                    _currentProfile.ADCSOptions.Status
                ),
                new(
                    "Supported Identifiers",
                    _currentProfile.SupportedIdentifiers.JoinOr(),
                    _currentProfile.SupportedIdentifiers.Length > 0 ? Status.AllGood : Status.NeedsAttention
                )
            ];
        }
    }
}

internal class ConfigRoot
{
    [JsonPropertyName("AcmeServer")]
    internal ACMEServerOptions ServerOptions { get; } = new();

    [JsonPropertyName("AcmeFileStore")]
    internal FileStoreOptions FileStoreOptions { get; } = new();

    [JsonPropertyName("DNS")]
    internal DNSOverrideOptions DNSOverrideOptions { get; } = new();

    [JsonPropertyName("Profiles")]
    internal ProfileOptions Profiles { get; } = new();


    public void BuildConfig(string filePath)
    {
        // Implementation for building the config file
    }

    internal class ProfileOptions
    {
        internal Dictionary<string, ProfileConfiguration> Items { get; set; } = [];
    }
}


internal class ProfileBuilder
{
    public string? ProfileName { get; internal set; }
}

internal static class OptionsExtensions
{
    extension(ACMEServerOptions options)
    {
        public Status CAAStatus => options.CAAIdentities.Length == 0
            ? Status.Recommended
            : Status.AllGood;
    }

    extension(FileStoreOptions options)
    {
        public Status Status => string.IsNullOrWhiteSpace(options.BasePath)
            ? Status.NeedsAttention
            : Status.AllGood;
    }

    extension(ConfigRoot.ProfileOptions options)
    {
        public Status Status => options.Items.Count == 0
            ? Status.NeedsAttention
            : Status.AllGood;
    }

    extension(ADCSOptions options)
    {
        public Status Status =>
            string.IsNullOrWhiteSpace(options.TemplateName) || string.IsNullOrWhiteSpace(options.CAServer)
                ? Status.NeedsAttention
                : Status.AllGood;
    }
}

internal static class StringExtensions
{
    extension(string? value)
    {
        public string? TrimOrNull()
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}