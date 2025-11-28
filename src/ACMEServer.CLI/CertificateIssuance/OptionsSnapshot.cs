using Microsoft.Extensions.Options;

namespace Th11s.ACMEServer.CLI.CertificateIssuance;


internal class OptionsSnapshot<T> : IOptionsSnapshot<T>
    where T : class
{
    private readonly Dictionary<string, T> _values;

    public OptionsSnapshot(Dictionary<string, T> values)
    {
        _values = values;
    }

    public T Value => Get("");

    public T Get(string? name)
        => _values[name ?? ""];
}
