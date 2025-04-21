using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

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
        if(authorization.Identifier.Type == IdentifierTypes.DNS)
        {
            _ = new Challenge(authorization, ChallengeTypes.Dns01);
            if (!authorization.IsWildcard)
            {
                _ = new Challenge(authorization, ChallengeTypes.Http01);
                _ = new Challenge(authorization, ChallengeTypes.TlsAlpn01);
            }
        }

        else if (authorization.Identifier.Type == IdentifierTypes.IP)
        {
            _ = new Challenge(authorization, ChallengeTypes.Http01);
            _ = new Challenge(authorization, ChallengeTypes.TlsAlpn01);
        }
        //else if (authorization.Identifier.Type == IdentifierTypes.Email)
        //{
        //    _ = new Challenge(authorization, ChallengeTypes.Smtp01);
        //}
        //else if (authorization.Identifier.Type == IdentifierTypes.PermanentIdentifier)
        //{
        //    _ = new Challenge(authorization, ChallengeTypes.DeviceAttest01);
        //}
        //else if (authorization.Identifier.Type == IdentifierTypes.HardwareModule)
        //{
        //    _ = new Challenge(authorization, ChallengeTypes.DeviceAttest01);
        //}
        else
        {
            throw new NotImplementedException($"Challenge for {authorization.Identifier.Type} not implemented");
        }
    }
}
