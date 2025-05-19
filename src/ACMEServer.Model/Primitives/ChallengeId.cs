namespace Th11s.ACMEServer.Model.Primitives;

public readonly struct ChallengeId
{
    public ChallengeId()
    {
        Value = GuidString.NewValue();
    }

    public ChallengeId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static implicit operator string(ChallengeId challengeId) => challengeId.Value;
    public static explicit operator ChallengeId(string value) => new(value);

    public override readonly string ToString() => Value;
    
    public override readonly bool Equals(object? obj) => obj is ChallengeId other && Value == other.Value;
    public override readonly int GetHashCode() => HashCode.Combine(Value);

    public static bool operator ==(ChallengeId left, ChallengeId right) => left.Equals(right);
    public static bool operator !=(ChallengeId left, ChallengeId right) => !left.Equals(right);
}
