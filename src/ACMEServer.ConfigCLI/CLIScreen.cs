namespace Th11s.ACMEServer.ConfigCLI;

internal abstract class CLIScreen(ConfigCLI parent)
{
    private readonly ConfigCLI _parent = parent;

    protected abstract string? ScreenTitle { get; }
    protected abstract string? ScreenDescription { get; }

    protected abstract List<CLIAction> Actions { get; }

    protected abstract void RenderCurrentConfig();

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

        for (int i = 0; i < Actions.Count; i++)
        {
            Console.WriteLine($"[{Actions[i].Key}]: {Actions[i].Description}");
        }

        CLIAction? selectedAction = null;
        do
        {
            Console.WriteLine();
            Console.Write("Select an option: ");

            var input = Console.ReadKey();
            if(input.Key == ConsoleKey.Escape)
            {
                selectedAction = CLIAction.BackOrQuit;
                break;
            }

            selectedAction = Actions.FirstOrDefault(a => char.ToLowerInvariant(a.Key) == char.ToLowerInvariant(input.KeyChar));
        }
        while (selectedAction is null);

        selectedAction.Execute(_parent);
    }
}
