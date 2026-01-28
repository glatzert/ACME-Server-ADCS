using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface IAuthorizationFactory
{
    void CreateAuthorizations(Order order, Dictionary<Identifier, string[]> allowedChallengeTypes);
}
