﻿using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Services.ChallengeValidation;

namespace Th11s.ACMEServer.Services
{
    public class DefaultChallengeValidatorFactory : IChallengeValidatorFactory
    {
        private readonly IServiceProvider _services;

        public DefaultChallengeValidatorFactory(IServiceProvider services)
        {
            _services = services;
        }

        public IChallengeValidator GetValidator(Challenge challenge)
        {
            if (challenge is null)
                throw new ArgumentNullException(nameof(challenge));

            IChallengeValidator validator = challenge.Type switch
            {
                ChallengeTypes.Http01 => _services.GetRequiredService<Http01ChallengeValidator>(),
                ChallengeTypes.Dns01 => _services.GetRequiredService<Dns01ChallengeValidator>(),
                ChallengeTypes.TlsAlpn01 => _services.GetRequiredService<TlsAlpn01ChallengeValidator>(),
                _ => throw new InvalidOperationException("Unknown Challenge Type")
            };

            return validator;
        }
    }
}
