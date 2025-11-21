namespace Th11s.ACMEServer.ConfigCLI;

internal class StorageConfigScreen(ConfigCLI parent, FileStoreConfigBuilder configBuilder) : CLIScreen(parent)
{
    private readonly FileStoreConfigBuilder _configBuilder = configBuilder;

    protected override string? ScreenTitle => "Storage Configuration";
    protected override string? ScreenDescription => "Configure the storage settings below.";

    protected override List<CLIAction> Actions =>
    [
        new CLIAction('S', "Set Base Path", ModifyBasePath),
        // Define actions for storage configuration here
        new CLIAction('B', "Back to Main Menu", Parent.PopScreen)
    ];

    private void ModifyBasePath()
    {
        var basePath = CLIPrompt.String("Enter the base path for file storage");
        _configBuilder.SetBasePath(basePath);
    }

    protected override List<ConfigInfo> GetConfigInfo()
        => [
            new(
                "Storage base path",
                _configBuilder.Options.BasePath ?? "n/a",
                _configBuilder.Status
            )
        ];
}
