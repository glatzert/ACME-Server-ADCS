using System.Text;

namespace Th11s.ACMEServer.Model.Extensions
{
    public static class LoggingExtensions
    {
        public static string AsLogString(this IEnumerable<Identifier> identifiers)
            => string.Join(",", identifiers.Select(id => $"{id.Type}:{id.Value}"));

    }
}
