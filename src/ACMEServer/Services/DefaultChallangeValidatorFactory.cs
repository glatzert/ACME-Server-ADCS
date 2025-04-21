using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class DefaultChallengeValidatorFactory(IEnumerable<IChallengeValidator> validators) : IChallengeValidatorFactory
{
    private readonly IEnumerable<IChallengeValidator> _validators = validators;

    public IChallengeValidator GetValidator(Challenge challenge)
    {
        ArgumentNullException.ThrowIfNull(challenge);

        var validator = _validators
            .Where(v => v.ChallengeType == challenge.Type)
            .Where(v => v.SupportedIdentiferTypes.Contains(challenge.Authorization.Identifier.Type))
            .FirstOrDefault() 
            ?? throw new InvalidOperationException($"No validator found for challenge type {challenge.Type} and identifier type {challenge.Authorization.Identifier.Type}");


        return validator;
    }
}
