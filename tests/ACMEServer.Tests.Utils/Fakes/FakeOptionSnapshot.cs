using Microsoft.Extensions.Options;

namespace ACMEServer.Tests.Utils.Fakes;

public class FakeOptionSnapshot<T> : IOptionsSnapshot<T>
    where T : class
{
    private readonly Dictionary<string, T> _values;

    public FakeOptionSnapshot(Dictionary<string, T> values)
    {
        _values = values;
    }

    public T Value => Get("");

    public T Get(string? name)
        => _values[name ?? ""];
}
