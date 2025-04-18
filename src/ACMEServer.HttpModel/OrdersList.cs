namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Represents a list of order urls
/// </summary>
public class OrdersList(IEnumerable<string> orders)
{
    public List<string> Orders { get; set; } = [.. orders];
}
