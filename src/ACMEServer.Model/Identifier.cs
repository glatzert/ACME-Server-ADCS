namespace Th11s.ACMEServer.Model;

public record Identifier(string Type, string Value)
{
    public override string ToString()
    {
        return $"{Type}:{Value}";
    }
}
