namespace Th11s.ACMEServer.ConfigCLI;

internal abstract class CLIScreen(ConfigCLI parent)
{
    protected abstract string? ScreenTitle { get; }
    protected abstract string? ScreenDescription { get; }

    protected abstract List<CLIAction> Actions { get; }

    protected ConfigCLI Parent { get; } = parent;

    protected abstract List<ConfigInfo> GetConfigInfo();

    public void Render()
    {
        Console.Clear();
        if (!string.IsNullOrEmpty(ScreenTitle))
        {
            Console.WriteLine(ScreenTitle);
            Console.WriteLine(new string('=', ScreenTitle.Length));
            Console.WriteLine();
        }

        if (!string.IsNullOrEmpty(ScreenDescription))
        {
            Console.WriteLine(ScreenDescription);
            Console.WriteLine();
        }

        RenderCurrentConfig();
        RenderActions();

        CLIAction? selectedAction = null;
        do
        {
            Console.WriteLine();
            Console.Write("Select an option: ");

            var input = Console.ReadKey();
            if (input.Key == ConsoleKey.Escape)
            {
                Parent.PopScreen();
                break;
            }

            selectedAction = Actions.FirstOrDefault(a => char.ToLowerInvariant(a.Key) == char.ToLowerInvariant(input.KeyChar));
        }
        while (selectedAction is null);

        Console.WriteLine();
        selectedAction?.Execute(Parent);
    }

    private void RenderCurrentConfig()
    {
        var configInfo = GetConfigInfo();
        foreach (var info in configInfo)
        {
            Console.WriteLine($"{info.Title}: {info.Value} {info.Status.ToSymbol()}");
            if (info.SubInfo is not null)
            {
                foreach (var subInfo in info.SubInfo)
                {
                    Console.WriteLine($"    {subInfo.Title}: {subInfo.Value} {subInfo.Status.ToSymbol()}");
                }
            }
        }

        Console.WriteLine();
    }

    private void RenderActions()
    {
        for (int i = 0; i < Actions.Count; i++)
        {
            var status = Actions[i].GetStatus(Parent);
            Console.ForegroundColor = status.SetConsoleColor(Console.ForegroundColor);

            Console.WriteLine($"{Actions[i].Key}: {Actions[i].Description} {status.ToSymbol()}");
            Console.ResetColor();
        }
    }
}

internal record ConfigInfo(string Title, string Value, Status Status)
{
    public List<ConfigInfo>? SubInfo { get; init; }
}