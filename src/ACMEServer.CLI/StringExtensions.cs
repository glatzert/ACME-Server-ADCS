namespace Th11s.ACMEServer.CLI;

internal static class StringExtensions
{
    extension(string? value)
    {
        public string? TrimOrNull()
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}