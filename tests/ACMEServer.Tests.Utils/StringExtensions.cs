using System.Text;

namespace Th11s.ACMEServer.Tests.Utils;

public static class StringExtensions
{
    extension(string s)
    {
        public Stream ToUtf8Stream()
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            return new MemoryStream(bytes);
        }
    }
}
