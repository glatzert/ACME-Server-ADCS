using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel.Payloads;

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

        builder.MapPost("/order/{orderId}/auth/{authorizationId}", GetAuthorization)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.GetAuthorization);

        builder.MapPost("/order/{orderId}/auth/{authorizationId}/chall/{challengeId}", AcceptChallenge)
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
        ILinkGenerator linkGenerator)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        if (!acmeRequest.TryGetPayload<CreateOrder>(out var orderRequest) || orderRequest is null)
        {
            throw new MalformedRequestException("Malformed request payload");
        }

        if (orderRequest.Identifiers is not { Count: > 0 })
            throw new MalformedRequestException("No identifiers submitted");

        foreach (var i in orderRequest.Identifiers)
            if (string.IsNullOrWhiteSpace(i.Type) || string.IsNullOrWhiteSpace(i.Value))
                throw new MalformedRequestException($"Malformed identifier: (Type: {i.Type}, Value: {i.Value})");

        var identifiers = orderRequest.Identifiers.Select(x =>
            new Model.Identifier(x.Type!, x.Value!)
        );

        var order = await _orderService.CreateOrderAsync(
            httpContext.User.GetAccountId(),
            httpContext.User.HasExternalAccountBinding(),
            orderRequest, 
            httpContext.RequestAborted);


        var orderResponse = GetOrderResponse(order, httpContext, linkGenerator);

        var orderUrl = linkGenerator.GetOrder(order.OrderId);
        return Results.Created(orderUrl, orderResponse);
    }

    private static HttpModel.Order GetOrderResponse(Model.Order order, HttpContext httpContext, ILinkGenerator linkGenerator)
    {
        var authorizations = order.Authorizations
            .Select(x => linkGenerator.GetAuthorization(order.OrderId, x.AuthorizationId));

        return new(order)
        {
            Authorizations = [..authorizations],
            Finalize = linkGenerator.FinalizeOrder(order.OrderId),
            Certificate = order.Status == Model.OrderStatus.Valid ? linkGenerator.GetCertificate(order.OrderId) : null
        };
    }

    public static async Task<IResult> GetOrder(string orderId, HttpContext httpContext, IOrderService orderService, ILinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var order = await orderService.GetOrderAsync(accountId, new(orderId), httpContext.RequestAborted);

        if (order == null)
            return Results.NotFound();

        var orderResponse = GetOrderResponse(order, httpContext, linkGenerator);
        return Results.Ok(orderResponse);
    }

    public static async Task<IResult> GetAuthorization(
        string orderId, 
        string authorizationId, 
        HttpContext httpContext, 
        IOrderService orderService, 
        ILinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var order = await orderService.GetOrderAsync(accountId, new(orderId), httpContext.RequestAborted);

        if (order == null)
            return Results.NotFound();

        var authZ = order.GetAuthorization(new(authorizationId));
        if (authZ == null)
            return Results.NotFound();

        var challenges = authZ.Challenges
            .Select(challenge =>
            {
                var challengeUrl = linkGenerator.GetChallenge(order.OrderId, authZ.AuthorizationId, challenge.ChallengeId);
                return HttpModel.Challenge.FromModel(challenge, challengeUrl);
            });

        var authZResponse = new HttpModel.Authorization(authZ, challenges);

        return Results.Ok(authZResponse);
    }


    public static async Task<IResult> AcceptChallenge(string orderId, string authorizationId, string challengeId, HttpContext httpContext, IOrderService orderService, ILinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var acmeRequest = httpContext.GetAcmeRequest();

        var challenge = await orderService.ProcessChallengeAsync(accountId, new(orderId), new(authorizationId), new (challengeId), acmeRequest, httpContext.RequestAborted) 
            ?? throw new NotFoundException();

        httpContext.AddLinkResponseHeader("up", linkGenerator.GetAuthorization(challenge.Authorization.Order.OrderId, challenge.Authorization.AuthorizationId));
        httpContext.AddLocationResponseHeader(linkGenerator.GetOrder(new(orderId)));

        var challengeResponse = HttpModel.Challenge.FromModel(challenge, linkGenerator.GetChallenge(challenge.Authorization.Order.OrderId, challenge.Authorization.AuthorizationId, challenge.ChallengeId));
        return Results.Ok(challengeResponse);
    }


    public static async Task<IResult> FinalizeOrder(string orderId, HttpContext httpContext, IOrderService orderService, ILinkGenerator linkGenerator)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        if (!acmeRequest.TryGetPayload<FinalizeOrder>(out var finalizeOrderRequest) || finalizeOrderRequest is null)
        {
            throw new MalformedRequestException("Malformed request payload");
        }

        var accountId = httpContext.User.GetAccountId();
        var order = await orderService.ProcessCsr(accountId, new(orderId), finalizeOrderRequest, httpContext.RequestAborted);

        httpContext.AddLocationResponseHeader(linkGenerator.GetOrder(order.OrderId));

        var orderResponse = GetOrderResponse(order, httpContext, linkGenerator);
        return Results.Ok(orderResponse);
    }


    public static async Task<IResult> GetCertificate(string orderId, IOrderService _orderService, HttpContext httpContext, ILinkGenerator linkGenerator)
    {
        var accountId = httpContext.User.GetAccountId();
        var orderCertificate = await _orderService.GetCertificate(accountId, new(orderId), httpContext.RequestAborted);

        if (orderCertificate == null)
            return Results.NotFound();

        httpContext.AddLocationResponseHeader(linkGenerator.GetOrder(new (orderId)));

        var pemChain = ToPEMCertificateChain(orderCertificate);
        return Results.File(Encoding.ASCII.GetBytes(pemChain), "application/pem-certificate-chain");
    }


    private static string ToPEMCertificateChain(byte[] orderCertificate)
    {
#if NET10_0_OR_GREATER
        var certificateCollection = X509CertificateLoader.LoadPkcs12Collection(orderCertificate, null);
#else
        var certificateCollection = new X509Certificate2Collection();
        certificateCollection.Import(orderCertificate);
#endif

        var stringBuilder = new StringBuilder();
        foreach (var certificate in certificateCollection)
        {
            var certPem = PemEncoding.Write("CERTIFICATE", certificate.Export(X509ContentType.Cert));
            stringBuilder.Append(certPem);
            stringBuilder.Append('\n');
        }

        return stringBuilder.ToString();
    }
}
