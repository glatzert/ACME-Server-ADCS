using Th11s.ACMEServer.ConfigCLI;
using Th11s.ACMEServer.ConfigCLI.ConfigTool;

namespace Th11s.ACMEServer.ConfigCLI.ConfigTool;

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
