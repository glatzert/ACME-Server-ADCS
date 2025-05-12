namespace Th11s.ACMEServer.Model.Primitives;

public readonly record struct ProfileName(string Value)
{
    public static implicit operator string(ProfileName accountId) => accountId.Value;
    public static explicit operator ProfileName(string value) => new(value);

    public override readonly string ToString() => Value;
    public override readonly int GetHashCode() => HashCode.Combine(Value);

    public static ProfileName None => new(string.Empty);
}