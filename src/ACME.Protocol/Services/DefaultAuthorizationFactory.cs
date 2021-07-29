using System;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public class DefaultAuthorizationFactory : IAuthorizationFactory
    {
        public void CreateAuthorizations(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));

            foreach(var identifier in order.Identifiers)
            {
                //TODO : set useful expiry;
                var authorization = new Authorization(order, identifier, DateTimeOffset.UtcNow.AddDays(2));
                CreateChallenges(authorization);
            }
        }

        private static void CreateChallenges(Authorization authorization)
        {
            _ = new Challenge(authorization, ChallengeTypes.Dns01);
            if (!authorization.IsWildcard)
                _ = new Challenge(authorization, ChallengeTypes.Http01);
        }
    }
}
