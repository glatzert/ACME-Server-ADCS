using Microsoft.Extensions.Options;

namespace Th11s.AcmeServer.Tests
{
    public class FakeOptionSnapshot<T> : IOptionsSnapshot<T>
        where T : class
    {
        private readonly IDictionary<string, T> _values;

        public FakeOptionSnapshot(IDictionary<string, T> values)
        {
            _values = values;
        }

        public T Value => Get("");

        public T Get(string? name)
            => _values[name ?? ""];
    }
}
