using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.JWS;

public static class AcmeJwsHeaderExtensions
{
    public static AccountId GetAccountIdFromKid(this AcmeJwsHeader header)
    {
        if (header.Kid == null)
        {
            throw new InvalidOperationException();
        }

        return new(header.Kid.Split('/').Last());
    }
}