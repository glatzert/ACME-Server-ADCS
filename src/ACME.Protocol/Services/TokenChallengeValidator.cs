using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public abstract class TokenChallengeValidator : IChallengeValidator
    {
        protected abstract Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken);
        protected abstract string GetExpectedContent(Challenge challenge, Account account);

        public async Task<(bool IsValid, AcmeError? error)> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
        {
            if (challenge is null)
                throw new ArgumentNullException(nameof(challenge));
            if (account is null)
                throw new ArgumentNullException(nameof(account));

            if (account.Status != AccountStatus.Valid)
                return (false, new AcmeError("unauthorized", "Account invalid", challenge.Authorization.Identifier));

            if (challenge.Authorization.Expires < DateTimeOffset.UtcNow)
            {
                challenge.Authorization.SetStatus(AuthorizationStatus.Expired);
                return (false, new AcmeError("custom:authExpired", "Authorization expired", challenge.Authorization.Identifier));
            }
            if (challenge.Authorization.Order.Expires < DateTimeOffset.UtcNow)
            {
                challenge.Authorization.Order.SetStatus(OrderStatus.Invalid);
                return (false, new AcmeError("custom:orderExpired", "Order expired"));
            }

            var (challengeContent, error) = await LoadChallengeResponseAsync(challenge, cancellationToken);
            if (error != null)
                return (false, error);

            var expectedResponse = GetExpectedContent(challenge, account);
            if(challengeContent?.Contains(expectedResponse) != true)
                return (false, new AcmeError("incorrectResponse", "Challenge response dod not contain the expected content.", challenge.Authorization.Identifier));

            return (true, null);
        }
    }
}
