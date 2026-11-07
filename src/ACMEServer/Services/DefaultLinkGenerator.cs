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

        public string NewNonce() => GetUrlByName(EndpointNames.NewNonce);

        public string NewAccount() => GetUrlByName(EndpointNames.NewAccount);

        public string GetAccount(AccountId accountId) => GetUrlByName(EndpointNames.GetAccount, new { accountId = accountId.Value });

        public string KeyChange() => GetUrlByName(EndpointNames.KeyChange);

        public string GetOrderList(AccountId accountId) => GetUrlByName(EndpointNames.GetOrderList, new { accountId = accountId.Value });

        public string NewOrder() => GetUrlByName(EndpointNames.NewOrder);

        public string GetOrder(OrderId order) => GetUrlByName(EndpointNames.GetOrder, new { orderId = order.Value });

        public string GetAuthorization(OrderId orderId, AuthorizationId authorizationId) => GetUrlByName(EndpointNames.GetAuthorization, new { orderId = orderId.Value, authorizationId = authorizationId.Value });

        public string GetChallenge(OrderId orderId, AuthorizationId authorizationId, ChallengeId challengeId) => GetUrlByName(EndpointNames.AcceptChallenge, new { orderId = orderId.Value, authorizationId = authorizationId.Value, challengeId = challengeId.Value });

        public string FinalizeOrder(OrderId orderId) => GetUrlByName(EndpointNames.FinalizeOrder, new { orderId = orderId.Value });

        public string GetCertificate(OrderId orderId) => GetUrlByName(EndpointNames.GetCertificate, new { orderId = orderId.Value });

        public string RevokeCert() => GetUrlByName(EndpointNames.RevokeCert);

        public string RenewalInfo() => GetUrlByName(EndpointNames.RenewalInfo);

        public string ProfileMetadata(ProfileName profileName) => GetUrlByName(EndpointNames.ProfileMetadata, new { profile = profileName.Value });
        

        public string AccountHash()
        {
            var (host, pathBase, scheme) = GetBaseUrl();
            return new Uri($"{scheme}://{host}{pathBase}/{_serverOptions.Value.DnsPersistPreProvisioning?.AccountHashPrefix ?? "account-hash"}").ToString();
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

        private string GetUrlByName(string endpointName, object? values = null)
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
