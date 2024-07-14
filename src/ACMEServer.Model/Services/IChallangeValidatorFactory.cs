namespace Th11s.ACMEServer.Model.Services
{
    public interface IChallangeValidatorFactory
    {
        IChallengeValidator GetValidator(Challenge challenge);
    }
}
