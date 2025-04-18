namespace Th11s.ACMEServer.HttpModel;

/// <summary>
/// Represents a list of order urls
/// </summary>
public class OrdersList
{
    public OrdersList(IEnumerable<string> orders)
    {
        Orders = orders.ToList();
    }

    public List<string> Orders { get; set; }
}
