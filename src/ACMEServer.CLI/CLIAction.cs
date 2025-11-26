using Th11s.ACMEServer.CLI.ConfigTool;

namespace Th11s.ACMEServer.CLI;

internal class CLIAction(
    char key,
    string description,
    Action action,
    Func<Status>? status = null)
{
    private readonly Action _action = action;
    private readonly Func<Status>? _status = status;

    public char Key { get; } = key;
    public string Description { get; } = description;
    
    public void Execute(ConfigCLI parent) => _action.Invoke();
    public Status GetStatus(ConfigCLI parent) => _status?.Invoke() ?? Status.None;
}
