namespace Th11s.ACMEServer.ConfigCLI;

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