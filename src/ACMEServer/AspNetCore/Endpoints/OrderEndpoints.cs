using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel.Payloads;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/new-order", CreateOrder)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.NewOrder);

        builder.MapPost("/order/{orderId}", GetOrder)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.GetOrder);

        builder.MapPost("/order/{orderId}/auth/{authId}", GetAuthorization)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.GetAuthorization);

        builder.MapPost("/order/{orderId}/auth/{authId}/chall/{challengeId}", AcceptChallenge)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.AcceptChallenge);

        builder.MapPost("/order/{orderId}/finalize", FinalizeOrder)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.FinalizeOrder);

        builder.MapPost("/order/{orderId}/certificate", GetCertificate)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.GetCertificate);


        return builder;
    }


    public static async Task<IResult> CreateOrder(
        HttpContext httpContext,
        IOrderService _orderService, 
        LinkGenerator linkGenerator)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        if (!acmeRequest.TryGetPayload<CreateOrder>(out var orderRequest) || orderRequest is null)
        {
            throw new MalformedRequestException("Malformed request payload");
        }

        if (orderRequest.Identifiers?.Any() != true)
            throw new MalformedRequestException("No identifiers submitted");

        foreach (var i in orderRequest.Identifiers)
            if (string.IsNullOrWhiteSpace(i.Type) || string.IsNullOrWhiteSpace(i.Value))
                throw new MalformedRequestException($"Malformed identifier: (Type: {i.Type}, Value: {i.Value})");

        var identifiers = orderRequest.Identifiers.Select(x =>
            new Model.Identifier(x.Type!, x.Value!)
        );

        var order = await _orderService.CreateOrderAsync(httpContext.User.GetAccountId(), orderRequest, httpContext.RequestAborted);


        var orderResponse = GetOrderResponse(order, httpContext, linkGenerator);

        var orderUrl = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrder, new { orderId = order.OrderId });
        return Results.Created(orderUrl, orderResponse);
    }

    private static HttpModel.Order GetOrderResponse(Model.Order order, HttpContext httpContext, LinkGenerator linkGenerator)
        => new(order) {
            Authorizations = order.Authorizations
                .Select(x => linkGenerator.GetUriByName(httpContext, EndpointNames.GetAuthorization ,new { orderId = order.OrderId, authId = x.AuthorizationId })!)
                .ToList(),
            Finalize = linkGenerator.GetUriByName(httpContext, EndpointNames.FinalizeOrder, new { orderId = order.OrderId }),
            Certificate = order.Status == OrderStatus.Valid ? linkGenerator.GetUriByName(httpContext, EndpointNames.GetCertificate, new { orderId = order.OrderId }) : null
        };


    public static async Task<IResult> GetOrder(string orderId, HttpContext httpContext, IOrderService orderService, LinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var order = await orderService.GetOrderAsync(accountId, orderId, httpContext.RequestAborted);

        if (order == null)
            return Results.NotFound();

        var orderResponse = GetOrderResponse(order, httpContext, linkGenerator);
        return Results.Ok(orderResponse);
    }

    public static async Task<IResult> GetAuthorization(string orderId, string authId, HttpContext httpContext, IOrderService orderService, LinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var order = await orderService.GetOrderAsync(accountId, orderId, httpContext.RequestAborted);

        if (order == null)
            return Results.NotFound();

        var authZ = order.GetAuthorization(authId);
        if (authZ == null)
            return Results.NotFound();

        var challenges = authZ.Challenges
            .Select(challenge =>
            {
                var challengeUrl = GetChallengeUrl(challenge, httpContext, linkGenerator);

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


    public static async Task<IResult> AcceptChallenge(string orderId, string authId, string challengeId, HttpContext httpContext, IOrderService orderService, LinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var challenge = await orderService.ProcessChallengeAsync(accountId, orderId, authId, challengeId, httpContext.RequestAborted) 
            ?? throw new NotFoundException();

        httpContext.AddLinkResponseHeader(linkGenerator, "up", EndpointNames.GetAuthorization, new { orderId = orderId, authId = authId });
        httpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetOrder, new { orderId = orderId });

        var challengeResponse = new HttpModel.Challenge(challenge, GetChallengeUrl(challenge, httpContext, linkGenerator));
        return Results.Ok(challengeResponse);
    }


    public static async Task<IResult> FinalizeOrder(string orderId, HttpContext httpContext, IOrderService orderService, LinkGenerator linkGenerator)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        if (!acmeRequest.TryGetPayload<FinalizeOrder>(out var finalizeOrderRequest) || finalizeOrderRequest is null)
        {
            throw new MalformedRequestException("Malformed request payload");
        }

        var accountId = httpContext.User.GetAccountId();
        var order = await orderService.ProcessCsr(accountId, orderId, finalizeOrderRequest, httpContext.RequestAborted);

        httpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetOrder, new { orderId = orderId });

        var orderResponse = GetOrderResponse(order, httpContext, linkGenerator);
        return Results.Ok(orderResponse);
    }


    public static async Task<IResult> GetCertificate(string orderId, IOrderService _orderService, HttpContext httpContext, LinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var orderCertificate = await _orderService.GetCertificate(accountId, orderId, httpContext.RequestAborted);

        if (orderCertificate == null)
            return Results.NotFound();

        httpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetOrder, new { orderId = orderId });

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
