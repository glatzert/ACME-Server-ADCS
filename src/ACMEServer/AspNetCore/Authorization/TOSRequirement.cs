using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Th11s.ACMEServer.AspNetCore.Authentication;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.AspNetCore.Authorization
{
    public class TOSRequirement : IAuthorizationRequirement
    {
        public TOSRequirement() { }
    }

    public class TOSRequirementHandler : AuthorizationHandler<TOSRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<ACMEServerOptions> _options;

        public TOSRequirementHandler(IHttpContextAccessor httpContextAccessor, IOptions<ACMEServerOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TOSRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.CompletedTask;
            }

            // Check if the user is authenticated
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // The server does not require a TOS agreement
            if (!_options.Value.TOS.RequireAgreement)
            {
                context.Succeed(requirement);
            }
            else
            {
                var userAgreement = httpContext.User.FindFirstValue(AcmeClaimTypes.TOSAcceptedAt);
                var lastTOSUpdate = _options.Value.TOS.LastUpdate;

                // The user did never agree
                if (userAgreement == null || !DateTimeOffset.TryParse(userAgreement, out var userAgreementDate))
                {
                    httpContext.Items.Add("acme-error", AcmeErrors.UserActionRequired("Terms of service need to be accepted."));
                    context.Fail();
                }
                else
                {
                    // The user agreed to the TOS, but the TOS has changed
                    if (lastTOSUpdate.HasValue && userAgreementDate.ToLocalTime() < lastTOSUpdate.Value)
                    {
                        // TODO: add a link to the new TOS
                        httpContext.Items.Add("acme-error", AcmeErrors.UserActionRequired($"Terms of service have changed at {lastTOSUpdate:O} need to be accepted again."));
                        context.Fail();
                    }
                    else
                    {
                        context.Succeed(requirement);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}