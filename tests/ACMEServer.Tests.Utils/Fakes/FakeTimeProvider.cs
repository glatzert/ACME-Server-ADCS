namespace Th11s.ACMEServer.Tests.Utils.Fakes;

public class FakeTimeProvider(DateTimeOffset fixedDateTime) : TimeProvider
{
    private readonly DateTimeOffset _fixedDateTime = fixedDateTime;

    /// <summary>
    /// Creates a fake time provider with 01.05.2025 15:37:00 UTC+1 as the fixed time.
    /// </summary>
    public FakeTimeProvider()
        : this(new DateTimeOffset(2025, 1, 5, 14, 37, 00, TimeSpan.Zero))
    { }

    public override DateTimeOffset GetUtcNow()
        => _fixedDateTime;
}
