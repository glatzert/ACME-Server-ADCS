using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class DefaultAuthorizationFactory(
    TimeProvider timeProvider,
    IOptions<ACMEServerOptions> options
    ) : IAuthorizationFactory
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptions<ACMEServerOptions> _options = options;

    public void CreateAuthorizations(Order order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        var expiryDate = _timeProvider.GetUtcNow().Add(_options.Value.AuthorizationValidityPeriod);
        foreach (var identifier in order.Identifiers)
        {
            var authorization = new Authorization(order, identifier, expiryDate);
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
