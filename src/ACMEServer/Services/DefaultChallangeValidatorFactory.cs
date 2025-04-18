using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services
{
    public class DefaultChallengeValidatorFactory : IChallengeValidatorFactory
    {
        private readonly IEnumerable<IChallengeValidator> _validators;

        public DefaultChallengeValidatorFactory(IEnumerable<IChallengeValidator> validators)
        {
            _validators = validators;
        }

        public IChallengeValidator GetValidator(Challenge challenge)
        {
            if (challenge is null)
                throw new ArgumentNullException(nameof(challenge));

            IChallengeValidator validator = challenge.Type switch
            {
                ChallengeTypes.Http01 => _validators.First(x => x.ChallengeType == ChallengeTypes.Http01),
                ChallengeTypes.Dns01 => _validators.First(x => x.ChallengeType == ChallengeTypes.Dns01),
                ChallengeTypes.TlsAlpn01 => _validators.First(x => x.ChallengeType == ChallengeTypes.TlsAlpn01),
                _ => throw new InvalidOperationException("Unknown Challenge Type")
            };

            return validator;
        }
    }
}
