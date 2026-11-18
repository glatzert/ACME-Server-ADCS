namespace Th11s.ACMEServer.Configuration;

public class QueueConfiguration
{
    public int SyncValidationTimeout { get; set; } = 500;
    public int SyncIssuanceTimeout { get; set; } = 500;
}
