namespace Th11s.ACMEServer.ConfigCLI;

public class ConfigCLI
{
    internal ConfigBuilder ConfigBuilder { get; } = new();
    private readonly Stack<CLIScreen> _screenStack = new();

    public async Task RunAsync()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        _screenStack.Push(new MainMenuScreen(this, ConfigBuilder));
        while (_screenStack.Count > 0)
        {
            var currentScreen = _screenStack.Peek();

            currentScreen.Render();
        }

        Console.Clear();
        DumpConfig();
    }

    private void DumpConfig()
    {
        Console.WriteLine("TODO: Dump the final configuration here.");
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
