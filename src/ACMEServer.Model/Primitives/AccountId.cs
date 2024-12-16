namespace Th11s.ACMEServer.Model.Primitives;

public readonly struct AccountId
{
    public AccountId()
    {
        Value = GuidString.NewValue();
    }

    public AccountId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static implicit operator string(AccountId accountId) => accountId.Value;
    public static explicit operator AccountId(string value) => new(value);

    public override readonly string ToString() => Value;
    
    public override readonly bool Equals(object? obj) => obj is AccountId other && Value == other.Value;
    public override readonly int GetHashCode() => HashCode.Combine(Value);

    public static bool operator ==(AccountId left, AccountId right) => left.Equals(right);
    public static bool operator !=(AccountId left, AccountId right) => !left.Equals(right);
}
