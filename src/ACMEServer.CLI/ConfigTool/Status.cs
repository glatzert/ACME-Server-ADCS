namespace Th11s.ACMEServer.CLI.ConfigTool;

internal enum Status
{
    None,
    AllGood,
    Recommended,
    NeedsAttention,
}

static internal class StatusExtensions
{
    public static string ToSymbol(this Status status) => status switch
    {
        Status.AllGood => "✅",
        Status.Recommended => "⚠️",
        Status.NeedsAttention => "🔴",
        _ => " "
    };

    public static ConsoleColor SetConsoleColor(this Status status, ConsoleColor foregroundColor)
        => status switch
        {
            Status.AllGood => ConsoleColor.Green,
            Status.Recommended => ConsoleColor.Yellow,
            Status.NeedsAttention => ConsoleColor.Red,
            _ => foregroundColor
        };
}