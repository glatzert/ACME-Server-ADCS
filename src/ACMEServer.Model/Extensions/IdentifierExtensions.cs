namespace Th11s.ACMEServer.Model.Extensions
{
    public static class IdentifierExtensions
    {
        public static bool IsWildcard(this Identifier identifier)
            => identifier.Type == IdentifierTypes.DNS && identifier.Value.StartsWith("*.");
    }
}
