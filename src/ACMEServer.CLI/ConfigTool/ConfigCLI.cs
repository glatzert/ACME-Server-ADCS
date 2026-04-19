using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Th11s.ACMEServer.CLI.ConfigTool;

public class ConfigCLI
{
    private readonly Stack<CLIScreen> _screenStack = new();
    private FileInfo? _targetFile;

    internal ConfigRoot ConfigRoot { get; } 

    internal ConfigCLI(FileInfo? targetFile, ConfigRoot configRoot)
    {
        _targetFile = targetFile;
        ConfigRoot = configRoot;
    }

    public async Task RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        _screenStack.Push(new MainMenuScreen(this, ConfigRoot));
        while (_screenStack.Count > 0)
        {
            var currentScreen = _screenStack.Peek();

            currentScreen.Render();
        }

        Console.Clear();
        await FinalizeProcess();
    }

    private async Task FinalizeProcess()
    {
        var configJson = JsonSerializer.Serialize(ConfigRoot.BuildSerializableConfig(), new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault | JsonIgnoreCondition.WhenWritingNull,
        });

        Console.WriteLine("Your configuration:");
        Console.WriteLine(configJson);

        do
        {
            if (_targetFile is null)
            {
                var saveToFile = CLIPrompt.Bool("Do you want to save the configuration to a file?");
                if (!saveToFile)
                {
                    break;
                }

                if (saveToFile)
                {
                    var filePath = CLIPrompt.String("Enter the file path to save the configuration [./appsettings.Production.json]:");
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        var assemblyPath = AppContext.BaseDirectory;
                        _targetFile = new(Path.Combine(assemblyPath, "appsettings.Production.json"));
                    }
                }
            }

            if (_targetFile is not null)
            {
                var allowFileWrite = !_targetFile.Exists;

                if (!allowFileWrite)
                {
                    Console.WriteLine($"File {_targetFile.FullName} already exists.");
                    allowFileWrite = CLIPrompt.Bool("Do you want to overwrite it?");
                }

                if (allowFileWrite)
                {
                    _targetFile.Directory.Create();
                    using var writer = _targetFile.CreateText();
                    await writer.WriteAsync(configJson);
                    await writer.FlushAsync();

                    Console.WriteLine($"Configuration saved to {_targetFile.FullName}");

                    break;
                }
            }
        }
        while (true);
    }

    internal void PushScreen(CLIScreen screen)
    {
        _screenStack.Push(screen);
    }

    internal void PopScreen()
    {
        if (_screenStack.Count > 0)
        {
            _screenStack.Pop();
        }
    }
}
