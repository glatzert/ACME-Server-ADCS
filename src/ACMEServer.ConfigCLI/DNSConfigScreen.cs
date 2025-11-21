namespace Th11s.ACMEServer.ConfigCLI;

internal class DNSConfigScreen(ConfigCLI parent, DNSConfigBuilder configBuilder) : CLIScreen(parent)
{
    private readonly DNSConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "DNS Overrides Configuration";
    protected override string? ScreenDescription => "Configure the DNS overrides below.";
    protected override List<CLIAction> Actions => [
        new CLIAction('E', "Edit Name Servers", ModifyNameServers),
        new CLIAction('B', "Back to Main Menu", Parent.PopScreen)
    ];

    private void ModifyNameServers()
    {
        var nameServers = CLIPrompt.StringList("Enter name server ip-addresses", _configBuilder.NameServers.ToList());
        _configBuilder.SetNameServer(nameServers);
    }

    protected override List<ConfigInfo> GetConfigInfo()
    {
        if (_configBuilder.NameServers.Length > 0)
        {
            return [
                new ConfigInfo("Name servers", _configBuilder.NameServers.JoinOr(), Status.None)
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
