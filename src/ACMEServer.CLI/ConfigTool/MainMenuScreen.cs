using Th11s.ACMEServer.CLI;
using Th11s.ACMEServer.CLI.ConfigTool;

namespace Th11s.ACMEServer.CLI.ConfigTool;

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
            _configRoot.ServerOptions.GetConfigInfo(),
            new(
                "Storage",
                _configRoot.FileStoreOptions.BasePath ?? "n/a",
                _configRoot.FileStoreOptions.Status
            ),
            new(
                "Profiles",
                _configRoot.Profiles.Select(x => x.Name).JoinOr(),
                _configRoot.Profiles.Status
            ),
            new(
                "DNS",
                _configRoot.DNSOverrideOptions.NameServers.Count > 0 ? "overriden" : "system default",
                Status.None
            )
        ];
}
