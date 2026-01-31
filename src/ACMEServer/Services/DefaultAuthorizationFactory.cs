using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Services;

public class DefaultAuthorizationFactory(
    TimeProvider timeProvider,
    IProfileProvider profileProvider,
    ILogger<DefaultAuthorizationFactory> logger
    ) : IAuthorizationFactory
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IProfileProvider _profileProvider = profileProvider;
    private readonly ILogger<DefaultAuthorizationFactory> _logger = logger;

    public void CreateAuthorizations(Order order, Dictionary<Identifier, string[]> caaAllowedChallengeTypes)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (!_profileProvider.TryGetProfileConfiguration(order.Profile, out var profileConfiguration))
        {
            _logger.LogError("Profile configuration for profile '{Profile}' not found.", order.Profile.Value);
            throw AcmeErrors.ServerInternal().AsException();
        }

        var expiryDate = _timeProvider.GetUtcNow().Add(profileConfiguration.AuthorizationValidityPeriod);
        foreach (var identifier in order.Identifiers)
        {
            var authorization = new Authorization(order, identifier, expiryDate);
            List<string> allowedChallengeTypes = authorization.Identifier.Type switch
            {
                IdentifierTypes.DNS when authorization.IsWildcard => [ChallengeTypes.Dns01],
                IdentifierTypes.DNS => [ChallengeTypes.Dns01, ChallengeTypes.Http01, ChallengeTypes.TlsAlpn01],
                IdentifierTypes.IP => [ChallengeTypes.Http01, ChallengeTypes.TlsAlpn01],
                IdentifierTypes.PermanentIdentifier => [ChallengeTypes.DeviceAttest01],
                IdentifierTypes.HardwareModule => [ChallengeTypes.DeviceAttest01],
                _ => throw new NotImplementedException($"Challenge for {authorization.Identifier.Type} not implemented"),
            };

            if(caaAllowedChallengeTypes.TryGetValue(identifier, out var caaChallenges))
            {
                allowedChallengeTypes = [.. allowedChallengeTypes.Intersect(caaChallenges)];
            }

            CreateChallenges(authorization, allowedChallengeTypes);
        }
    }

    private void CreateChallenges(Authorization authorization, List<string> allowedChallengeTypes)
    {
        if (allowedChallengeTypes.Count == 0)
        {
            _logger.LogInformation("No challenge types available for identifier {identifier} and its metadata restrictions {allowedChallengeTypes}", authorization.Identifier, string.Join(",", allowedChallengeTypes));
            throw AcmeErrors.NoChallengeTypeAvailable(authorization.Identifier, authorization.Order.Profile).AsException();
        }

        // Create the challenges
        foreach (var challengeType in allowedChallengeTypes)
        {
            if(challengeType == ChallengeTypes.DeviceAttest01)
            {
                _ = new DeviceAttestChallenge(authorization);
            }
            else if (ChallengeTypes.TokenChallenges.Contains(challengeType)) { 
                _ = new TokenChallenge(authorization, challengeType);
            }
        }
    }
}
