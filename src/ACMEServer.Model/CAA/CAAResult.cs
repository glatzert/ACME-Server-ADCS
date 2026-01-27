using System.Collections;

namespace Th11s.ACMEServer.Model.CAA;

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
