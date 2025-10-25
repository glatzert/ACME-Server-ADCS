using Microsoft.Extensions.Options;
using System.Text.Json;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Services;

public class DefaultAuthorizationFactory(
    TimeProvider timeProvider,
    IOptionsSnapshot<ProfileConfiguration> options
    ) : IAuthorizationFactory
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptionsSnapshot<ProfileConfiguration> _options = options;

    public void CreateAuthorizations(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var options = _options.Get(order.Profile.Value);

        var expiryDate = _timeProvider.GetUtcNow().Add(options.AuthorizationValidityPeriod);
        foreach (var identifier in order.Identifiers)
        {
            var authorization = new Authorization(order, identifier, expiryDate);
            CreateChallenges(authorization);
        }
    }

    private static void CreateChallenges(Authorization authorization)
    {
        List<string> challengeTypes = [];

        if(authorization.Identifier.Type == IdentifierTypes.DNS)
        {
            challengeTypes.Add(ChallengeTypes.Dns01);

            if(!authorization.IsWildcard)
            {
                challengeTypes.Add(ChallengeTypes.Http01);
                challengeTypes.Add(ChallengeTypes.TlsAlpn01);
            }
        }

        else if (authorization.Identifier.Type == IdentifierTypes.IP)
        {
            challengeTypes.Add(ChallengeTypes.Http01);
            challengeTypes.Add(ChallengeTypes.TlsAlpn01);
        }
        //else if (authorization.Identifier.Type == IdentifierTypes.Email)
        //{
        //    _ = new Challenge(authorization, ChallengeTypes.Smtp01);
        //}
        else if (authorization.Identifier.Type == IdentifierTypes.PermanentIdentifier)
        {
            challengeTypes.Add(ChallengeTypes.DeviceAttest01);
        }
        else if (authorization.Identifier.Type == IdentifierTypes.HardwareModule)
        {
            challengeTypes.Add(ChallengeTypes.DeviceAttest01);
        }
        else
        {
            throw new NotImplementedException($"Challenge for {authorization.Identifier.Type} not implemented");
        }


        var challengeTypeRestrictions = GetMetadataChallengeTypes(authorization.Identifier);
        if (challengeTypeRestrictions.Length > 0)
        {
            challengeTypes = challengeTypes.Intersect(challengeTypeRestrictions).ToList();
        }

        if (challengeTypes.Count == 0)
        {
            throw new InvalidOperationException($"No challenge types available for identifier {authorization.Identifier} and its metadata restrictions {string.Join(",", challengeTypeRestrictions)}.");
        }

        // Create the challenges
        foreach (var challengeType in challengeTypes)
        {
            _ = new Challenge(authorization, challengeType);
        }
    }

    private static string[] GetMetadataChallengeTypes(Identifier identifier)
    {
        var challengeTypesJson = identifier.Metadata.GetValueOrDefault(Identifier.MetadataKeys.CAAValidationMehods);
        if (challengeTypesJson is null)
        {
            return [];
        }
        var challengeTypes = JsonSerializer.Deserialize<string[]>(challengeTypesJson);
        return challengeTypes ?? [];
    }
}
