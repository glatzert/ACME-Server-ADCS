using System.Text;
using Th11s.ACMEServer.CLI;
using Th11s.ACMEServer.CLI.ConfigTool;

namespace Th11s.ACMEServer.CLI;

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

        if (!string.IsNullOrWhiteSpace(ScreenTitle) && !string.IsNullOrWhiteSpace(ScreenDescription))
        {

            if (!string.IsNullOrEmpty(ScreenTitle))
            {
                Console.WriteLine(ScreenTitle);
                Console.WriteLine(new string('=', ScreenTitle.Length));
            }

            if (!string.IsNullOrEmpty(ScreenDescription))
            {
                Console.WriteLine();
                Console.WriteLine(ScreenDescription);
            }

            RenderSeparator();
        }

        RenderCurrentConfig();
        RenderSeparator();
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
        var configInfos = GetConfigInfo();
        RenderInfoList(configInfos);
    }

    private void RenderInfoList(List<ConfigInfo>? configInfos, int indentLevel = 0)
    {
        if (configInfos is null)
        {
            return;
        }

        foreach (var info in configInfos)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < indentLevel; i++)
            {
                if (i != indentLevel)
                {
                    sb.Append("| ");
                }
                else
                {
                    sb.Append("|-");
                }
            }

            if(info.Title is not null)
            {
                sb.Append($"{info.Title}: ");
            }

            sb.Append(info.Value);
            sb.Append($" {info.Status.ToSymbol()}");

            Console.WriteLine(sb.ToString());
            RenderInfoList(info.SubInfo, indentLevel + 1);
        }

        {
            var sb = new StringBuilder();
            for (int i = 1; i <= indentLevel; i++)
            {
                if (i != indentLevel)
                {
                    sb.Append("| ");
                }
                else
                {
                    sb.Append("|___");
                }
            }

            Console.WriteLine(sb.ToString());
        }
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

    private void RenderSeparator()
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', Console.BufferWidth));
        Console.WriteLine();
    }
}

internal record ConfigInfo(string? Title = null, string Value = "", Status Status = Status.None)
{
    public List<ConfigInfo>? SubInfo { get; init; }
}