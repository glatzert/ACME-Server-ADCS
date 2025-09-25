namespace Th11s.ACMEServer.Model.Primitives;

public readonly record struct ProfileName(string Value)
{
    public ProfileName() : this(string.Empty)
    { }

    public override readonly string ToString() => Value;
    public override readonly int GetHashCode() => HashCode.Combine(Value);

    public static ProfileName None => new();
}