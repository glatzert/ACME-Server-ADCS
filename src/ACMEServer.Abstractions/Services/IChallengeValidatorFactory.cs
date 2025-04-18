using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface IChallengeValidatorFactory
{
    IChallengeValidator GetValidator(Challenge challenge);
}
