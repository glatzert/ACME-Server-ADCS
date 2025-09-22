using System.Security.Claims;
using Th11s.ACMEServer.AspNetCore.Authentication;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.AspNetCore.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static AccountId GetAccountId(this ClaimsPrincipal principal)
    {
        var accountIdClaim = principal.Claims.FirstOrDefault(c => c.Type == AcmeClaimTypes.AccountId);
        if (accountIdClaim == null)
        {
            throw new InvalidOperationException("No account ID found in claims.");
        }

        return new(accountIdClaim.Value);
    }

    public static bool HasExternalAccountBinding(this ClaimsPrincipal principal)
    {
        return principal.Claims.Any(c => c.Type == AcmeClaimTypes.ExternalAccountBinding);
    }
}
