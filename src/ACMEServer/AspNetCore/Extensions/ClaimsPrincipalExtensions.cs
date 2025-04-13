using System.Security.Claims;
using Th11s.ACMEServer.AspNetCore.Authentication;

namespace Th11s.ACMEServer.AspNetCore.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetAccountId(this ClaimsPrincipal principal)
        {
            var accountIdClaim = principal.Claims.FirstOrDefault(c => c.Type == AcmeClaimTypes.AccountId);
            if (accountIdClaim == null)
            {
                throw new InvalidOperationException("No account ID found in claims.");
            }

            return accountIdClaim.Value;
        }
    }
}
