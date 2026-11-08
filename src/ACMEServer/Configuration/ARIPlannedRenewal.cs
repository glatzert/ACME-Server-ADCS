namespace Th11s.ACMEServer.Configuration;

public class ARIPlannedRenewal
{
    public DateTimeOffset NotBeforeOlderThan { get; set; }

    public DateTimeOffset RenewalStart { get; set; }
    public TimeSpan? RenewalPeriod { get; set; }

    public string? ProfileName { get; set; }

    public string? ExplanationUrl { get; set; }
}