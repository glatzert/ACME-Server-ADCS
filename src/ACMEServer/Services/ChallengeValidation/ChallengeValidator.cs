
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

public abstract class ChallengeValidator(ILogger logger) : IChallengeValidator
{
    private readonly ILogger _logger = logger;

    public async Task<ChallengeValidationResult> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(account);

        _logger.AttemptingToValidateChallenge(challenge.ChallengeId, challenge.Type);

        if (account.Status != AccountStatus.Valid)
        {
            _logger.AccountNotValidForChallenge(account.AccountId);
            return new(ChallengeResult.Invalid, new AcmeError("unauthorized", "Account invalid") { Identifier = challenge.Authorization.Identifier });
        }

        if (challenge.Authorization.Expires < DateTimeOffset.UtcNow)
        {
            _logger.ChallengeAuthorizationExpired();
            challenge.Authorization.SetStatus(AuthorizationStatus.Expired);
            return new(ChallengeResult.Invalid, new AcmeError("custom:authExpired", "Authorization expired") { Identifier = challenge.Authorization.Identifier });
        }
        if (challenge.Authorization.Order.Expires < DateTimeOffset.UtcNow)
        {
            _logger.ChallengeOrderExpired();
            challenge.Authorization.Order.SetStatus(OrderStatus.Invalid);
            return new(ChallengeResult.Invalid, new AcmeError("custom:orderExpired", "Order expired"));
        }

        return await ValidateChallengeInternalAsync(challenge, account, cancellationToken);
    }

    public abstract string ChallengeType { get; }
    public abstract IEnumerable<string> SupportedIdentiferTypes { get; }

    protected abstract Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken);

    public static string GetKeyAuthToken(TokenChallenge challenge, Account account)
    {
        var thumbprintBytes = account.Jwk.SecurityKey.ComputeJwkThumbprint();
        var thumbprint = Base64UrlEncoder.Encode(thumbprintBytes);

        var keyAuthToken = $"{challenge.Token}.{thumbprint}";
        return keyAuthToken;
    }

    public static byte[] GetKeyAuthDigest(TokenChallenge challenge, Account account)
    {
        var keyAuthBytes = Encoding.UTF8.GetBytes(GetKeyAuthToken(challenge, account));
        var digestBytes = SHA256.HashData(keyAuthBytes);

        return digestBytes;
    }
}
