using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.HttpModel.Requests;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Server.Filters;

namespace TGIT.ACME.Server.Controllers
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
        public async Task<ActionResult<Protocol.HttpModel.Order>> CreateOrder(AcmePayload<CreateOrderRequest> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);

            var orderRequest = payload.Value;

            if (orderRequest.Identifiers?.Any() != true)
                throw new MalformedRequestException("No identifiers submitted");

            foreach (var i in orderRequest.Identifiers)
                if(string.IsNullOrWhiteSpace(i.Type) || string.IsNullOrWhiteSpace(i.Value))
                    throw new MalformedRequestException($"Malformed identifier: (Type: {i.Type}, Value: {i.Value})");

            var identifiers = orderRequest.Identifiers.Select(x =>
                new Protocol.Model.Identifier(x.Type!, x.Value!)
            );

            var order = await _orderService.CreateOrderAsync(
                account, identifiers,
                orderRequest.NotBefore, orderRequest.NotAfter,
                HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            var orderUrl = Url.RouteUrl("GetOrder", new { orderId = order.OrderId }, "https");
            return new CreatedResult(orderUrl, orderResponse);
        }

        private void GetOrderUrls(Order order, out IEnumerable<string> authorizationUrls, out string finalizeUrl, out string certificateUrl)
        {
            authorizationUrls = order.Authorizations
                .Select(x => Url.RouteUrl("GetAuthorization", new { orderId = order.OrderId, authId = x.AuthorizationId }, "https"));
            finalizeUrl = Url.RouteUrl("FinalizeOrder", new { orderId = order.OrderId }, "https");
            certificateUrl = Url.RouteUrl("GetCertificate", new { orderId = order.OrderId }, "https");
        }

        [Route("/order/{orderId}", Name = "GetOrder")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Order>> GetOrder(string orderId)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            if (order == null)
                return NotFound();

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);
            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);

            return orderResponse;
        }

        [Route("/order/{orderId}/auth/{authId}", Name = "GetAuthorization")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.Authorization>> GetAuthorization(string orderId, string authId)
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

                    return new Protocol.HttpModel.Challenge(challenge, challengeUrl);
                });

            var authZResponse = new Protocol.HttpModel.Authorization(authZ, challenges);

            return authZResponse;
        }

        private string GetChallengeUrl(Challenge challenge)
        {
            return Url.RouteUrl("AcceptChallenge",
                new { 
                    orderId = challenge.Authorization.Order.OrderId,
                    authId = challenge.Authorization.AuthorizationId,
                    challengeId = challenge.ChallengeId },
                "https");
        }

        [Route("/order/{orderId}/auth/{authId}/chall/{challengeId}", Name = "AcceptChallenge")]
        [HttpPost]
        [AcmeLocation("GetOrder")]
        public async Task<ActionResult<Protocol.HttpModel.Challenge>> AcceptChallenge(string orderId, string authId, string challengeId)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var challenge = await _orderService.ProcessChallengeAsync(account, orderId, authId, challengeId, HttpContext.RequestAborted);

            if (challenge == null)
                throw new NotFoundException();

            var challengeResponse = new Protocol.HttpModel.Challenge(challenge, GetChallengeUrl(challenge));
            return challengeResponse;
        }

        [Route("/order/{orderId}/finalize", Name = "FinalizeOrder")]
        [HttpPost]
        [AcmeLocation("GetOrder")]
        public async Task<ActionResult<Protocol.HttpModel.Order>> FinalizeOrder(string orderId, AcmePayload<FinalizeOrderRequest> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.ProcessCsr(account, orderId, payload.Value.Csr, HttpContext.RequestAborted);

            GetOrderUrls(order, out var authorizationUrls, out var finalizeUrl, out var certificateUrl);

            var orderResponse = new Protocol.HttpModel.Order(order, authorizationUrls, finalizeUrl, certificateUrl);
            return orderResponse;
        }

        [Route("/order/{orderId}/certificate", Name = "GetCertificate")]
        [HttpPost]
        [AcmeLocation("GetOrder")]
        public async Task<IActionResult> GetCertificate(string orderId)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var certificate = await _orderService.GetCertificate(account, orderId, HttpContext.RequestAborted);

            return File(certificate, "application/pem-certificate-chain");
        }
    }
}
