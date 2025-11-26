using System.Text.Json;

namespace Th11s.ACMEServer.CLI.ConfigTool;

public class ConfigCLI
{
    internal ConfigRoot ConfigRoot { get; } = new();
    private readonly Stack<CLIScreen> _screenStack = new();

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
        FinalizeProcess();
    }

    private void FinalizeProcess()
    {
        var configJson = JsonSerializer.Serialize(ConfigRoot, new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null
        });

        Console.WriteLine("Your configuration:");
        Console.WriteLine(configJson);

        var saveToFile = CLIPrompt.Bool("Do you want to save the configuration to a file?");
        if (saveToFile)
        {
            var filePath = CLIPrompt.String("Enter the file path to save the configuration [./appsettings.Production.json]:");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                var assemblyPath = AppContext.BaseDirectory;
                filePath = Path.Combine(assemblyPath, "appsettings.Production.json");
            }

            File.WriteAllText(filePath, configJson);
            Console.WriteLine($"Configuration saved to {filePath}");
        }
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
