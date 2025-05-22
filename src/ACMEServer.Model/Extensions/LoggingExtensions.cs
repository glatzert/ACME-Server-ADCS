using System.Text;

namespace Th11s.ACMEServer.Model.Extensions
{
    public static class LoggingExtensions
    {
        public static string AsLogString(this IList<Identifier> identifiers)
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for(int i = 0; i < identifiers.Count; i++)
            {
                var identifier = identifiers[i];
                sb.Append(identifier.Type);
                sb.Append(":");
                sb.Append(identifier.Value);

                if (i < identifiers.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }
    }
}
