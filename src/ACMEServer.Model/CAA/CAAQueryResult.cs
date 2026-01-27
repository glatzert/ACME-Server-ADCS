namespace Th11s.ACMEServer.Model.CAA;

public class CAAQueryResult
{
    public required string CAIdentifier { get; init; }
    public required string Tag { get; init; }
    public required CAAFlags Flags { get; init; }
    public required string[] Parameters { get; init; }

    public static CAAQueryResult Parse(string tag, byte flags, string value)
    {
        var identifierAndParameters = value.Split(';', StringSplitOptions.TrimEntries);
        var identifier = identifierAndParameters.First();
        var parameters = identifierAndParameters.Skip(1).ToArray();

        return new CAAQueryResult()
        {
            CAIdentifier = identifier,
            Tag = tag,
            Flags = (CAAFlags)flags,

            Parameters = parameters
        };
    }
}
