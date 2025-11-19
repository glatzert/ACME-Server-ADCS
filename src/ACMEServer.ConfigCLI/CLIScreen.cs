using System.Net.NetworkInformation;

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
            var status = Actions[i].GetStatus(_parent);
            
            if(status == ActionStatus.AllGood)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else if(status == ActionStatus.Recommended)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if(status == ActionStatus.NeedsAttention)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine($"[{Actions[i].Key}]: {Actions[i].Description}");
            Console.ResetColor();
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
