using ACMEServer.Storage.FileSystem.Configuration;

namespace Th11s.ACMEServer.ConfigCLI.ConfigTool;

internal class StorageConfigScreen(ConfigCLI parent, FileStoreOptions options) : CLIScreen(parent)
{
    private readonly FileStoreOptions _options = options;

    protected override string? ScreenTitle => "Storage Configuration";
    protected override string? ScreenDescription => "Configure the storage settings below.";

    protected override List<CLIAction> Actions =>
    [
        new CLIAction('S', "Set Base Path", ModifyBasePath),
        new CLIAction('B', "Back to Main Menu", Parent.PopScreen)
    ];

    private void ModifyBasePath()
    {
        var basePath = CLIPrompt.String("Enter the base path for file storage");
        _options.BasePath = basePath;
    }

    protected override List<ConfigInfo> GetConfigInfo()
        => [
            new(
                "Storage base path",
                _options.BasePath ?? "n/a",
                _options.Status
            )
        ];
}
