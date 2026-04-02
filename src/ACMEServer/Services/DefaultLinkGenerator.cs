using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services
{
    public class DefaultLinkGenerator(
        IOptions<ACMEServerOptions> serverOptions,
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator,
        ILogger<DefaultLinkGenerator> logger
    ) : ILinkGenerator
    {
        private readonly IOptions<ACMEServerOptions> _serverOptions = serverOptions;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly LinkGenerator _linkGenerator = linkGenerator;
        private readonly ILogger<DefaultLinkGenerator> _logger = logger;

        public string NewNonce()
        {
            return GetUrlByName(EndpointNames.NewNonce);
        }

        public string NewAccount()
        {
            return GetUrlByName(EndpointNames.NewAccount);
        }

        public string GetAccount(AccountId accountId)
        {
            return GetUrlByName(EndpointNames.GetAccount, new { accountId });
        }

        public string KeyChange()
        {
            return GetUrlByName(EndpointNames.KeyChange);
        }

        public string GetOrderList(AccountId accountId)
        {
            return GetUrlByName(EndpointNames.GetOrderList, new { accountId = accountId.Value });
        }


        public string NewOrder()
        {
            return GetUrlByName(EndpointNames.NewOrder);
        }

        public string GetOrder(OrderId order)
        {
            return GetUrlByName(EndpointNames.GetOrder, new { orderId = order.Value });
        }

        public string? GetAuthorization(OrderId orderId, AuthorizationId authorizationId)
        {
            return GetUrlByName(EndpointNames.GetAuthorization, new { orderId = orderId.Value, authorizationId = authorizationId.Value });
        }

        public string GetChallenge(OrderId orderId, AuthorizationId authorizationId, ChallengeId challengeId)
        {
            return GetUrlByName(EndpointNames.AcceptChallenge, new { orderId = orderId.Value, authorizationId = authorizationId.Value, challengeId = challengeId.Value });
        }

        public string FinalizeOrder(OrderId orderId)
        {
            return GetUrlByName(EndpointNames.FinalizeOrder, new { orderId = orderId.Value });
        }

        public string GetCertificate(OrderId orderId)
        {
            return GetUrlByName(EndpointNames.GetCertificate, new { orderId = orderId.Value });
        }

        public string RevokeCert()
        {
            return GetUrlByName(EndpointNames.RevokeCert);
        }

        public string ProfileMetadata(ProfileName profileName)
        {
            return GetUrlByName(EndpointNames.Profile, new { profile = profileName.Value });
        }

        private (HostString host, PathString PathBase, string scheme) GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;

            var hostName = _serverOptions.Value.CanonicalHostname
                ?? _serverOptions.Value.CAAIdentities.FirstOrDefault() 
                ?? request?.Host.ToString()
                ?? throw new InvalidOperationException("Neither canonical hostname nor CAAIdentities configured and no Host header in request to determine base URL");

            var scheme = request?.Scheme ?? "https"; // Default to https if no HttpContext is available, as ACME servers should be served over HTTPS
            var basePath = request?.PathBase ?? "";

            return (new(hostName), basePath, scheme);
        }

        private string? GetUrlByName(string endpointName, object? values = null)
        {
            var (host, pathBase, scheme) = GetBaseUrl();
            var url = _linkGenerator.GetUriByName(endpointName, values, scheme, host, pathBase);

            if (url is null)
            {
                _logger.CouldNotCreateUrl(endpointName);
                throw new InvalidOperationException($"Could not create URL for endpoint '{endpointName}' with values '{values}'.");
            }

            return url;
        }
    }
}
