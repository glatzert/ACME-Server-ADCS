namespace Th11s.ACMEServer.Model.Primitives;

public readonly struct ChallangeId
{
    public ChallangeId()
    {
        Value = GuidString.NewValue();
    }

    public ChallangeId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static implicit operator string(ChallangeId challengeId) => challengeId.Value;
    public static explicit operator ChallangeId(string value) => new(value);

    public override readonly string ToString() => Value;
    
    public override readonly bool Equals(object? obj) => obj is ChallangeId other && Value == other.Value;
    public override readonly int GetHashCode() => HashCode.Combine(Value);

    public static bool operator ==(ChallangeId left, ChallangeId right) => left.Equals(right);
    public static bool operator !=(ChallangeId left, ChallangeId right) => !left.Equals(right);
}
