namespace Th11s.ACMEServer.Model.Primitives;

public readonly struct AuthorizationId
{
    public AuthorizationId()
    {
        Value = GuidString.NewValue();
    }

    public AuthorizationId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static implicit operator string(AuthorizationId authorizationId) => authorizationId.Value;
    public static explicit operator AuthorizationId(string value) => new(value);

    public override readonly string ToString() => Value;
    
    public override readonly bool Equals(object? obj) => obj is AuthorizationId other && Value == other.Value;
    public override readonly int GetHashCode() => HashCode.Combine(Value);

    public static bool operator ==(AuthorizationId left, AuthorizationId right) => left.Equals(right);
    public static bool operator !=(AuthorizationId left, AuthorizationId right) => !left.Equals(right);
}
