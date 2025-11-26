using Th11s.ACMEServer.CLI;
using Th11s.ACMEServer.ConfigCLI;

namespace Th11s.ACMEServer.CLI;

public static class RenderExtensions
{
    extension(IEnumerable<string> strings)
    {
        public string JoinOr(string separator = ",", string fallback = "none")
        {
            if (strings.Any())
            {
                return string.Join(separator, strings);
            }

            return fallback;
        }
    }
}