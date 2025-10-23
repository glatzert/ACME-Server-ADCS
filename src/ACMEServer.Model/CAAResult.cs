using System.Collections;

namespace Th11s.ACMEServer.Model
{
    public class CAAQueryResults(IReadOnlyList<CAAQueryResult> items) : IReadOnlyList<CAAQueryResult>
    {
        public CAAQueryResult this[int index] => Items[index];

        public IReadOnlyList<CAAQueryResult> Items { get; } = items;

        public int Count => Items.Count;

        public IEnumerator<CAAQueryResult> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Items).GetEnumerator();
        }
    }

    public class CAAQueryResult
    {
        public required string Domain { get; init; }
        public required string Tag { get; init; }
        public required CAAFlags Flags { get; init; }
        public required string[] Parameters { get; init; }

        public static CAAQueryResult Parse(string tag, byte flags, string value)
        {
            var domainAndParameters = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var domain = domainAndParameters.First();
            var parameters = domainAndParameters.Skip(1).ToArray();

            return new CAAQueryResult()
            {
                Domain = domain,
                Tag = tag,
                Flags = (CAAFlags)flags,

                Parameters = parameters
            };
        }
    }

    public enum CAAFlags
    {
        None = 0,
        IssuerCritical = 128
    }
}
