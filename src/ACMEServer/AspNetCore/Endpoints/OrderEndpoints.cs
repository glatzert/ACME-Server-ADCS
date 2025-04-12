using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints
{
    public static class OrderEndpoints
    {
        public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder builder)
        {
            builder.MapPost("/new-order", CreateOrder)
                .RequireAuthorization()
                .WithName(EndpointNames.NewOrder);

            builder.MapPost("/order/{orderId}", GetOrder)
                .RequireAuthorization()
                .WithName(EndpointNames.GetOrder);

            builder.MapPost("/order/{orderId}/auth/{authId}", GetAuthorization)
                .RequireAuthorization()
                .WithName(EndpointNames.GetAuthorization);

            builder.MapPost("/order/{orderId}/auth/{authId}/chall/{challengeId}", AcceptChallenge)
                .RequireAuthorization()
                .WithName(EndpointNames.AcceptChallenge);

            builder.MapPost("/order/{orderId}/finalize", FinalizeOrder)
                .RequireAuthorization()
                .WithName(EndpointNames.FinalizeOrder);

            builder.MapPost("/order/{orderId}/certificate", GetCertificate)
                .RequireAuthorization()
                .WithName(EndpointNames.GetCertificate);


            return builder;
        }


        public static async Task<IResult> CreateOrder(AcmePayload<CreateOrderRequest> payload, IAccountService _accountService, IOrderService _orderService, HttpContext HttpContext, LinkGenerator linkGenerator)
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


            var orderResponse = GetOrderResponse(order, HttpContext, linkGenerator);

            var orderUrl = linkGenerator.GetUriByName(HttpContext, EndpointNames.GetOrder, new { orderId = order.OrderId });
            return Results.Created(orderUrl, orderResponse);
        }

        private static HttpModel.Order GetOrderResponse(Model.Order order, HttpContext httpContext, LinkGenerator linkGenerator)
            => new HttpModel.Order(order) {
                Authorizations = order.Authorizations
                    .Select(x => linkGenerator.GetUriByName(httpContext, EndpointNames.GetAuthorization ,new { orderId = order.OrderId, authId = x.AuthorizationId })!)
                    .ToList(),
                Finalize = linkGenerator.GetUriByName(httpContext, EndpointNames.FinalizeOrder, new { orderId = order.OrderId }),
                Certificate = order.Status == OrderStatus.Valid ? linkGenerator.GetUriByName(httpContext, EndpointNames.GetCertificate, new { orderId = order.OrderId }) : null
            };


        public static async Task<IResult> GetOrder(string orderId, IAccountService _accountService, IOrderService _orderService, HttpContext HttpContext, LinkGenerator linkGenerator)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            if (order == null)
                return Results.NotFound();

            var orderResponse = GetOrderResponse(order, HttpContext, linkGenerator);
            return Results.Ok(orderResponse);
        }

        public static async Task<IResult> GetAuthorization(string orderId, string authId, IAccountService _accountService, IOrderService _orderService, HttpContext HttpContext, LinkGenerator linkGenerator)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.GetOrderAsync(account, orderId, HttpContext.RequestAborted);

            if (order == null)
                return Results.NotFound();

            var authZ = order.GetAuthorization(authId);
            if (authZ == null)
                return Results.NotFound();

            var challenges = authZ.Challenges
                .Select(challenge =>
                {
                    var challengeUrl = GetChallengeUrl(challenge, HttpContext, linkGenerator);

                    return new HttpModel.Challenge(challenge, challengeUrl);
                });

            var authZResponse = new HttpModel.Authorization(authZ, challenges);

            return Results.Ok(authZResponse);
        }

        private static string GetChallengeUrl(Model.Challenge challenge, HttpContext HttpContext, LinkGenerator linkGenerator) 
            => linkGenerator.GetUriByName(
                HttpContext,
                EndpointNames.AcceptChallenge,
                new
                {
                    orderId = challenge.Authorization.Order.OrderId,
                    authId = challenge.Authorization.AuthorizationId,
                    challengeId = challenge.ChallengeId
                })!;

        public static async Task<IResult> AcceptChallenge(string orderId, string authId, string challengeId, IAccountService _accountService, IOrderService _orderService, HttpContext HttpContext, LinkGenerator linkGenerator)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var challenge = await _orderService.ProcessChallengeAsync(account, orderId, authId, challengeId, HttpContext.RequestAborted);

            if (challenge == null)
                throw new NotFoundException();

            HttpContext.AddLinkResponseHeader(linkGenerator, "up", EndpointNames.GetAuthorization, new { orderId = orderId, authId = authId });
            HttpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetOrder, new { orderId = orderId });

            var challengeResponse = new HttpModel.Challenge(challenge, GetChallengeUrl(challenge, HttpContext, linkGenerator));
            return Results.Ok(challengeResponse);
        }

        public static async Task<IResult> FinalizeOrder(string orderId, AcmePayload<FinalizeOrderRequest> payload, IAccountService _accountService, IOrderService _orderService, HttpContext HttpContext, LinkGenerator linkGenerator)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var order = await _orderService.ProcessCsr(account, orderId, payload.Value.Csr, HttpContext.RequestAborted);

            HttpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetOrder, new { orderId = orderId });

            var orderResponse = GetOrderResponse(order, HttpContext, linkGenerator);
            return Results.Ok(orderResponse);
        }

        public static async Task<IResult> GetCertificate(string orderId, IAccountService _accountService, IOrderService _orderService, HttpContext HttpContext, LinkGenerator linkGenerator)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var orderCertificate = await _orderService.GetCertificate(account, orderId, HttpContext.RequestAborted);

            if (orderCertificate == null)
                return Results.NotFound();

            HttpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetOrder, new { orderId = orderId });

            var pemChain = ToPEMCertificateChain(orderCertificate);
            return Results.File(Encoding.ASCII.GetBytes(pemChain), "application/pem-certificate-chain");
        }

        private static string ToPEMCertificateChain(byte[] orderCertificate)
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
