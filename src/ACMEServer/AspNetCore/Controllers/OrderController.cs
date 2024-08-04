using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.AspNetCore.Filters;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.AspNetCore.Controllers
{
    [AddNextNonce]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IAccountService _accountService;

        public OrderController(IOrderService orderService, IAccountService accountService)
        {
            _orderService = orderService;
            _accountService = accountService;
        }

        [Route("/new-order", Name = "NewOrder")]
        [HttpPost]
        public async Task<ActionResult<HttpModel.Order>> CreateOrder(AcmePayload<CreateOrderRequest> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);

            var orderRequest = payload.Value;

            if (orderRequest.Identifiers?.Any() != true)
                throw new MalformedRequestException("No identifiers submitted");

            foreach (var i in orderRequest.Identifiers)
                if (string.IsNullOrWhiteSpace(i.Type) || string.IsNullOrWhiteSpace(i.Value))
                    throw new MalformedRequestException($"Malformed identifier: (Type: {i.Type}, Value: {i.Value})");

            var identifiers = orderRequest.Identifiers.Select(x =>
                new Model.Identifier(x.Type!, x.Value!)
            );

            var order = await _orderService.CreateOrderAsync(
                account, identifiers,
                orderRequest.NotBefore, orderRequest.NotAfter,
                HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            var orderUrl = Url.RouteUrl("GetOrder", new { orderId = order.OrderId }, HttpContext.GetProtocol());
            return new CreatedResult(orderUrl, orderResponse);
        }

        private void GetOrderUrls(Model.Order order, out IEnumerable<string> authorizationUrls, out string finalizeUrl, out string certificateUrl)
        {
            authorizationUrls = order.Authorizations
                .Select(x => Url.RouteUrl("GetAuthorization", new { orderId = order.OrderId, authId = x.AuthorizationId }, HttpContext.GetProtocol()));
            finalizeUrl = Url.RouteUrl("FinalizeOrder", new { orderId = order.OrderId }, HttpContext.GetProtocol());
            certificateUrl = Url.RouteUrl("GetCertificate", new { orderId = order.OrderId }, HttpContext.GetProtocol());
        }

        [Route("/order/{orderId}", Name = "GetOrder")]
        [HttpPost]
        public async Task<ActionResult<HttpModel.Order>> GetOrder(string orderId)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            if (order == null)
                return NotFound();

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            return orderResponse;
        }

        [Route("/order/{orderId}/auth/{authId}", Name = "GetAuthorization")]
        [HttpPost]
        public async Task<ActionResult<HttpModel.Authorization>> GetAuthorization(string orderId, string authId)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            if (order == null)
                return NotFound();

            var authZ = order.GetAuthorization(authId);
            if (authZ == null)
                return NotFound();

            var challenges = authZ.Challenges
                .Select(challenge =>
                {
                    var challengeUrl = GetChallengeUrl(challenge);

                    return new HttpModel.Challenge(challenge, challengeUrl);
                });

            var authZResponse = new HttpModel.Authorization(authZ, challenges);

            return authZResponse;
        }

        private string GetChallengeUrl(Model.Challenge challenge)
        {
            return Url.RouteUrl("AcceptChallenge",
                new
                {
                    orderId = challenge.Authorization.Order.OrderId,
                    authId = challenge.Authorization.AuthorizationId,
                    challengeId = challenge.ChallengeId
                },
                HttpContext.GetProtocol());
        }

        [Route("/order/{orderId}/auth/{authId}/chall/{challengeId}", Name = "AcceptChallenge")]
        [HttpPost]
        [AcmeLocation("GetOrder", "orderId")]
        public async Task<ActionResult<HttpModel.Challenge>> AcceptChallenge(string orderId, string authId, string challengeId)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var challenge = await _orderService.ProcessChallengeAsync(account, orderId, authId, challengeId, HttpContext.RequestAborted);

            if (challenge == null)
                throw new NotFoundException();

            var linkHeaderUrl = Url.RouteUrl("GetAuthorization", new { orderId, authId }, HttpContext.GetProtocol());
            var linkHeader = $"<{linkHeaderUrl}>;rel=\"up\"";

            HttpContext.Response.Headers.AddOrMerge("Link", linkHeader);

            var challengeResponse = new HttpModel.Challenge(challenge, GetChallengeUrl(challenge));
            return challengeResponse;
        }

        [Route("/order/{orderId}/finalize", Name = "FinalizeOrder")]
        [HttpPost]
        [AcmeLocation("GetOrder", "orderId")]
        public async Task<ActionResult<HttpModel.Order>> FinalizeOrder(string orderId, AcmePayload<FinalizeOrderRequest> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.ProcessCsr(account, orderId, payload.Value.Csr, HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);

            var orderResponse = new HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);
            return orderResponse;
        }

        [Route("/order/{orderId}/certificate", Name = "GetCertificate")]
        [HttpPost]
        [AcmeLocation("GetOrder", "orderId")]
        public async Task<IActionResult> GetCertificate(string orderId)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var orderCertificate = await _orderService.GetCertificate(account, orderId, HttpContext.RequestAborted);

            if (orderCertificate == null)
                return NotFound();

            var pemChain = ToPEMCertificateChain(orderCertificate);
            return File(Encoding.ASCII.GetBytes(pemChain), "application/pem-certificate-chain");
        }

        private string ToPEMCertificateChain(byte[] orderCertificate)
        {
            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(orderCertificate);

            var stringBuilder = new StringBuilder();
            foreach (var certificate in certificateCollection)
            {
                var certPem = PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert));
                stringBuilder.AppendLine(new string(certPem));
            }

            return stringBuilder.ToString();
        }
    }
}
