using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public interface IChallangeValidatorFactory
    {
        IChallengeValidator GetValidator(Challenge challenge);
    }
}
