namespace Th11s.ACMEServer.ConfigCLI;

internal class CLIAction(
    char key,
    string description,
    Action<ConfigCLI> action,
    Func<ConfigBuilder, ActionStatus?> status = null)
{
    private readonly Action<ConfigCLI> _action = action;
    private readonly Func<ConfigBuilder, ActionStatus?> _status = status;

    public char Key { get; } = key;
    public string Description { get; } = description;
    
    public void Execute(ConfigCLI parent) => _action.Invoke(parent);
    public ActionStatus GetStatus(ConfigCLI parent) => _status?.Invoke(parent.ConfigBuilder)
        ?? ActionStatus.None;

    public static CLIAction BackOrQuit => 
        new(
            'q',
            "Back / Quit",
            cli => cli.PopScreen(),
            _ => null
        );
}
