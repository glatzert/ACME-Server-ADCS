using Th11s.ACMEServer.CLI;
using Th11s.ACMEServer.CLI.ConfigTool;
using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.CLI.ConfigTool;

internal class DNSConfigScreen(ConfigCLI parent, DNSOverrideOptions options) : CLIScreen(parent)
{
    private readonly DNSOverrideOptions _options = options;

    protected override string? ScreenTitle => "DNS Overrides Configuration";
    protected override string? ScreenDescription => "Configure the DNS overrides below.";
    protected override List<CLIAction> Actions => [
        new CLIAction('E', "Edit Name Servers", ModifyNameServers),
        new CLIAction('B', "Back to Main Menu", Parent.PopScreen)
    ];

    private void ModifyNameServers()
    {
        var nameServers = CLIPrompt.StringList("Enter name server IP endpoints (e.g. 4.4.4.4:53)", [.. _options.NameServers]);
        _options.NameServers = [.. nameServers];
    }

    protected override List<ConfigInfo> GetConfigInfo()
    {
        if (_options.NameServers.Length > 0)
        {
            return [
                new ConfigInfo("Name servers", _options.NameServers.JoinOr(), Status.None)
            ];
        }
        else
        {
            return [
                new("Name servers", "Uses system default", Status.None)
            ];
        }
    }
}
