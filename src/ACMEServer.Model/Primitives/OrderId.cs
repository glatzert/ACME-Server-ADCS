namespace Th11s.ACMEServer.Model.Primitives;

public readonly struct OrderId
{
    public OrderId()
    {
        Value = GuidString.NewValue();
    }

    public OrderId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static implicit operator string(OrderId orderId) => orderId.Value;
    public static explicit operator OrderId(string value) => new(value);

    public override readonly string ToString() => Value;
    
    public override readonly bool Equals(object? obj) => obj is OrderId other && Value == other.Value;
    public override readonly int GetHashCode() => HashCode.Combine(Value);

    public static bool operator ==(OrderId left, OrderId right) => left.Equals(right);
    public static bool operator !=(OrderId left, OrderId right) => !left.Equals(right);
}
