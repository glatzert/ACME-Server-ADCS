namespace Th11s.ACMEServer.Configuration;

public class BackgroundServiceOptions
{
    public int ValidationCheckInterval { get; set; } = 60;
    public int IssuanceCheckInterval { get; set; } = 60;

    // TODO: Make these configurable in the future, but for now, we can just use a default value of 500ms.
    // We might want to move this to a more general configuration class in the future, but for now, we can just keep it here.
    public int SyncValidationTimeout { get; set; } = 500;
    public int SyncIssuanceTimeout { get; set; } = 500;
}
