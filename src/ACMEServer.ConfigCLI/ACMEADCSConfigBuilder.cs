
using ACMEServer.Storage.FileSystem.Configuration;
using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.ConfigCLI;

internal class MainMenuScreen(ConfigCLI parent, ConfigBuilder configBuilder)
    : CLIScreen(parent)
{
    private readonly ConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "ACME Server Configuration Builder";
    protected override string? ScreenDescription => "Welcome to the ACME Server Configuration Builder. Please select an option to configure different aspects of the server.";

    protected override List<CLIAction> Actions =>
    [
        new CLIAction('A', "Configure Server Settings", () => Parent.PushScreen(new ServerConfigScreen(Parent, _configBuilder.ServerConfigBuilder))),
        new CLIAction('S', "Configure Storage", () => Parent.PushScreen(new StorageConfigScreen(Parent, _configBuilder.FileStoreConfigBuilder))),
        new CLIAction('P', "Configure Profiles", () => Parent.PushScreen(new ProfilesConfigScreen(Parent, _configBuilder.ProfileConfigBuilders))),
        new CLIAction('D', "Configure DNS Overrides", () => Parent.PushScreen(new DNSConfigScreen(Parent, _configBuilder.DnsConfigBuilder))),

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
                        _configBuilder.ServerConfigBuilder.Options.CAAIdentities.JoinOr("none"),
                        _configBuilder.ServerConfigBuilder.CAAStatus
                    ),

                    new(
                        "Revokation support enabled",
                        $"{_configBuilder.ServerConfigBuilder.Options.SupportsRevokation}",
                        Status.None
                    ),

                    new(
                        "Terms of service",
                        _configBuilder.ServerConfigBuilder.Options.TOS.RequireAgreement ? "agreement required" : "not configured",
                        Status.None
                    ),
                    new(
                        "External account binding",
                        _configBuilder.ServerConfigBuilder.Options.ExternalAccountBinding is null ? "disabled" : "enabled",
                        Status.None
                    )
                ] 
            },
            new(
                "Storage",
                _configBuilder.FileStoreConfigBuilder.Options.BasePath ?? "n/a",
                _configBuilder.FileStoreConfigBuilder.Status
            ),
            new(
                "Profiles",
                _configBuilder.ProfileConfigBuilders.Select(x => x.ProfileName).JoinOr(),
                !_configBuilder.ProfileConfigBuilders.Any()
                    ? Status.NeedsAttention
                    : Status.AllGood
            ),
            new(
                "DNS",
                _configBuilder.DnsConfigBuilder.NameServers.Length > 0 ? "configured" : "Use system default",
                Status.None
            )
        ];
}

internal class EABConfigScreen(ConfigCLI parent, ServerConfigBuilder configBuilder) : CLIScreen(parent)
{
    private readonly ServerConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "External account binding";

    protected override string? ScreenDescription => "To use external account binding, it's neccessary to get the MAC Key from a remote endpoint. This can be configured here.";

    protected override List<CLIAction> Actions
    {
        get
        {
            List<CLIAction> result = [];
            if (_configBuilder.Options.ExternalAccountBinding is ExternalAccountBindingOptions eab)
            {
                result.AddRange([
                    new CLIAction('D', "Disable external account binding", ToggleEAB),
                    new CLIAction('R', "Required external account binding", ToggleEABRequirement),
                    new CLIAction(
                        'M',
                        "Set MAC retrieval url",
                        SetMACRetrievalUrl,
                        () => string.IsNullOrWhiteSpace(eab.MACRetrievalUrl) ? Status.NeedsAttention : Status.AllGood),
                    new CLIAction('S', "Set success signal url", SetSuccessSignalUrl),
                    new CLIAction('F', "Set failure signal url", SetFailureSignalUrl)
                ]);
            }
            else
            {
                result.Add(
                    new CLIAction('E', "Enable external account binding", ToggleEAB)
                );
            }

            result.Add(new CLIAction('B', "Back to Main Menu", Parent.PopScreen));

            return result;
        }
    }

    private void SetFailureSignalUrl()
    {
        var url = CLIPrompt.String("Type the url where a binding can be signaled as failure. Use {kid} as placeholder for the KID.");
        _configBuilder.Options.ExternalAccountBinding?.FailedSignalUrl = url;
    }

    private void SetSuccessSignalUrl()
    {
        var url = CLIPrompt.String("Type the url where a binding can be signaled as success. Use {kid} as placeholder for the KID.");
        _configBuilder.Options.ExternalAccountBinding?.SuccessSignalUrl = url;
    }

    private void SetMACRetrievalUrl()
    {
        var url = CLIPrompt.String("Type the url where a mac can be retrieved. Use {kid} as placeholder for the KID.");
        _configBuilder.Options.ExternalAccountBinding?.MACRetrievalUrl = url;
    }

    private void ToggleEABRequirement()
    {
        _configBuilder.Options.ExternalAccountBinding?.Required = _configBuilder.Options.ExternalAccountBinding.Required;
    }

    private void ToggleEAB()
    {
        if (_configBuilder.Options.ExternalAccountBinding is null)
        {
            _configBuilder.SetEABOptions(new() { MACRetrievalUrl = "" });
        }
        else
        {
            _configBuilder.SetEABOptions(null);
        }
    }

    protected override List<ConfigInfo> GetConfigInfo() {
        List<ConfigInfo> subInfos = [];

        var eab = _configBuilder.Options.ExternalAccountBinding;

        if (eab is not null)
        {
            subInfos.AddRange([
                new("Required", eab.Required ? "yes" : "no", Status.None),
                new("MAC retrieval url", eab.MACRetrievalUrl, Status.None),
                new("Success signal url", eab.SuccessSignalUrl ?? "n/a", Status.None),
                new("Failure signal url", eab.MACRetrievalUrl ?? "n/a", Status.None),
            ]);
        }

        var eabInfo = new ConfigInfo(
            "External account binding",
            eab is not null ? "enabled" : "disabled",
            eab is not null && string.IsNullOrWhiteSpace(eab.MACRetrievalUrl) ? Status.NeedsAttention : Status.AllGood
        )
        {
            SubInfo = subInfos
        };

        return [eabInfo];
    }
}

internal class ProfilesConfigScreen(ConfigCLI parent, List<ProfileBuilder> configBuilders) : CLIScreen(parent)
{
    private readonly List<ProfileBuilder> _configBuilders = configBuilders;

    protected override string? ScreenTitle => "Profiles Configuration";
    protected override string? ScreenDescription => "Configure the profiles below.";
    protected override List<CLIAction> Actions =>
    [
        // TODO Define actions for profiles configuration here
        new CLIAction('B', "Back to Main Menu", Parent.PopScreen)
    ];

    protected override List<ConfigInfo> GetConfigInfo()
    {
        throw new NotImplementedException();
    }
}

internal abstract class ConfigBuilderBase
{
    public virtual Status Status => Status.None;
}

internal class ConfigBuilder : ConfigBuilderBase
{
    internal ServerConfigBuilder ServerConfigBuilder { get; } = new();

    internal List<ProfileBuilder> ProfileConfigBuilders { get; } = [];

    internal FileStoreConfigBuilder FileStoreConfigBuilder { get; } = new();

    internal DNSConfigBuilder DnsConfigBuilder { get; } = new();

    public void BuildConfig(string filePath)
    {
        // Implementation for building the config file
    }
}


internal class ServerConfigBuilder : ConfigBuilderBase
{
    public ACMEServerOptions Options { get; } = new();
    public Status CAAStatus
        => !Options.CAAIdentities.Any() ? Status.Recommended : Status.AllGood;

    internal void SetCAAIdentities(string[] identities)
        => Options.CAAIdentities = identities;

    internal void ToggleRevokationSupport()
        => Options.SupportsRevokation = !Options.SupportsRevokation;

    internal void SetWebsiteUrl(string? url)
        => Options.WebsiteUrl = url;

    internal void SetTermsOfService(TermsOfServiceOptions? tosOptions)
        => Options.TOS = tosOptions ?? new();

    internal void SetEABOptions(ExternalAccountBindingOptions? eabOptions)
        => Options.ExternalAccountBinding = eabOptions;

    public override Status Status
        => (Options.CAAIdentities == null || Options.CAAIdentities.Length == 0)
            ? Status.Recommended
            : Status.AllGood;
}


internal class ProfileBuilder
{
    public string? ProfileName { get; internal set; }
}
