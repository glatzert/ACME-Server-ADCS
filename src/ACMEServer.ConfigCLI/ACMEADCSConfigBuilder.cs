namespace Th11s.ACMEServer.ConfigCLI;

internal class MainMenuScreen(ConfigCLI parent, ConfigBuilder configBuilder)
    : CLIScreen(parent)
{
    private readonly ConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "ACME Server Configuration Builder";
    protected override string? ScreenDescription => "Welcome to the ACME Server Configuration Builder. Please select an option to configure different aspects of the server.";

    protected override List<CLIAction> Actions => new()
    {
        new CLIAction('A', "Configure Server Settings", cli => cli.PushScreen(new ServerConfigScreen(cli, _configBuilder.ServerConfigBuilder))),
        new CLIAction('S', "Configure Storage", cli => cli.PushScreen(new StorageConfigScreen(cli, _configBuilder.FileStoreConfigBuilder))),
        new CLIAction('P', "Configure Profiles", cli => cli.PushScreen(new ProfilesConfigScreen(cli, _configBuilder.ProfileConfigBuilders))),
        new CLIAction('D', "Configure DNS Overrides", cli => cli.PushScreen(new DNSConfigScreen(cli, _configBuilder.DnsConfigBuilder))),

        new CLIAction('Q', "Quit and Save Configuration", cli => cli.PopScreen())
    };
    protected override void RenderCurrentConfig()
    {

    }
}

internal class ServerConfigScreen(ConfigCLI parent, ServerConfigBuilder configBuilder) : CLIScreen(parent)
{
    private readonly ServerConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "Server Configuration";
    protected override string? ScreenDescription => "Configure the server settings below.";

    protected override List<CLIAction> Actions => new()
    {
        // Define actions for server configuration here
        new CLIAction('B', "Back to Main Menu", cli => cli.PopScreen())
    };

    protected override void RenderCurrentConfig()
    {
        // Render current server configuration here
    }
}

internal class StorageConfigScreen(ConfigCLI parent, FileStoreConfigBuilder configBuilder) : CLIScreen(parent)
{
    private readonly FileStoreConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "Storage Configuration";
    protected override string? ScreenDescription => "Configure the storage settings below.";

    protected override List<CLIAction> Actions => new()
    {
        // Define actions for storage configuration here
        new CLIAction('B', "Back to Main Menu", cli => cli.PopScreen())
    };

    protected override void RenderCurrentConfig()
    {
        // Render current server configuration here
    }
}

internal class ProfilesConfigScreen(ConfigCLI parent, List<ProfileBuilder> configBuilders) : CLIScreen(parent)
{
    private readonly List<ProfileBuilder> _configBuilders = configBuilders;

    protected override string? ScreenTitle => "Profiles Configuration";
    protected override string? ScreenDescription => "Configure the profiles below.";
    protected override List<CLIAction> Actions => new()
    {
        // Define actions for profiles configuration here
        new CLIAction('B', "Back to Main Menu", cli => cli.PopScreen())
    };

    protected override void RenderCurrentConfig()
    {
        // Render current server configuration here
    }
}

internal class DNSConfigScreen(ConfigCLI parent, DNSConfigBuilder configBuilder) : CLIScreen(parent)
{
    private readonly DNSConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "DNS Overrides Configuration";
    protected override string? ScreenDescription => "Configure the DNS overrides below.";
    protected override List<CLIAction> Actions => new() { 
        new CLIAction('B', "Back to Main Menu", cli => cli.PopScreen()) 
    };

    protected override void RenderCurrentConfig()
    {
        // Render current server configuration here
    }
}

internal class ConfigBuilder
{
    internal ServerConfigBuilder ServerConfigBuilder { get; } = new();

    internal List<ProfileBuilder> ProfileConfigBuilders { get; } = new();

    internal FileStoreConfigBuilder FileStoreConfigBuilder { get; } = new();

    internal DNSConfigBuilder DnsConfigBuilder { get; } = new();

    public void BuildConfig(string filePath)
    {
        // Implementation for building the config file
    }
}

internal class ServerConfigBuilder
{
    public ServerConfigBuilder()
    {

    }

    public void BuildConfig(string filePath)
    {
        // Implementation for building the config file
    }
}

internal class ProfileBuilder
{

}

internal class FileStoreConfigBuilder
{
}

internal class DNSConfigBuilder
{
}