namespace Th11s.ACMEServer.Model.Services
{
    public interface IChallengeValidatorFactory
    {
        IChallengeValidator GetValidator(Challenge challenge);
    }
}
