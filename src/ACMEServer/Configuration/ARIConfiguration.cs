namespace Th11s.ACMEServer.Configuration;

public class ARIConfiguration
{
    public const double DefaultRenewalStartPercentage = 25.0;

    public TimeSpan RetryAfter { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// This supports a % value for the renewal start, or a fixed timespan.
    /// </summary>
    public string DefaultRenewalStart { get; set; } = $"{DefaultRenewalStartPercentage}%";

    public TimeSpan DefaultRenewalPeriod { get; set; } = TimeSpan.FromDays(1);


    public List<ARIPlannedRenewal> PlannedRenewals { get; set; } = new();


    private (bool IsTimespan, TimeSpan TimeSpan)? _defaultRenewalStartTimespan;
    private (bool IsPercentage, double Percentage)? _defaultRenewalStartPercentage;

    public bool TryGetDefaultRenewalTimespan(out TimeSpan timespan)
    {
        if (_defaultRenewalStartTimespan.HasValue)
        {
            timespan = _defaultRenewalStartTimespan.Value.TimeSpan;
            return _defaultRenewalStartTimespan.Value.IsTimespan;
        }

        var result = TimeSpan.TryParse(DefaultRenewalStart, out timespan);
        _defaultRenewalStartTimespan = (result, timespan);

        return result;
    }

    public bool TryGetDefaultRenewalPercentage(out double percentage)
    {
        if (_defaultRenewalStartPercentage.HasValue)
        {
            percentage = _defaultRenewalStartPercentage.Value.Percentage;
            return _defaultRenewalStartPercentage.Value.IsPercentage;
        }

        if (DefaultRenewalStart.EndsWith('%') && double.TryParse(DefaultRenewalStart.TrimEnd('%'), out percentage))
        {
            _defaultRenewalStartPercentage = (true, percentage);
            return true;
        }

        percentage = 0;
        return false;
    }
}
