using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.Services
{
    public class DefaultAuthorizationFactory : IAuthorizationFactory
    {
        public void CreateAuthorizations(Order order)
        {
            if (order is null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            foreach (var identifier in order.Identifiers)
            {
                //TODO : set useful expiry;
                var authorization = new Authorization(order, identifier, DateTimeOffset.UtcNow.AddDays(2));
                CreateChallenges(authorization);
            }
        }

        private static void CreateChallenges(Authorization authorization)
        {
            if(authorization.Identifier.Type == "dns")
            {
                _ = new Challenge(authorization, ChallengeTypes.Dns01);
                if (!authorization.IsWildcard)
                {
                    _ = new Challenge(authorization, ChallengeTypes.Http01);
                    _ = new Challenge(authorization, ChallengeTypes.TlsAlpn01);
                }
            }
        }
    }
}
