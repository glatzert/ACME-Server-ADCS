using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.HttpModel.Payloads;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/new-account", CreateOrGetAccount)
            .RequireAuthorization()
            .WithName(EndpointNames.NewAccount);

        builder.MapMethods("/account/{accountId}", [HttpMethods.Post, HttpMethods.Put], GetOrSetAccount)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.GetAccount);

        builder.MapPost("/account/{accountId}/orders", GetOrdersList)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.GetOrderList);

        builder.MapPost("key-change", ChangeAccountKey)
            .RequireAuthorization(Policies.ValidAccount)
            .WithName(EndpointNames.KeyChange);

        return builder;
    }


    public static async Task<IResult> CreateOrGetAccount(
        HttpContext httpContext,
        IAccountService accountService,
        LinkGenerator linkGenerator)
    {
        var acmeRequest = httpContext.GetAcmeRequest();
        if(!acmeRequest.TryGetPayload<CreateOrGetAccount>(out var payload) || payload is null)
            throw AcmeErrors.MalformedRequest("Payload was empty or could not be read.").AsException();

        Model.Account? account;
        if (payload.OnlyReturnExisting)
        {
            account = await accountService.FindAccountAsync(acmeRequest.AcmeHeader.Jwk!, httpContext.RequestAborted)
                ?? throw AcmeErrors.AccountDoesNotExist().AsException();
        }
        else
        {
            account = await accountService.CreateAccountAsync(acmeRequest.AcmeHeader, payload, httpContext.RequestAborted);
        }

        var accountResponse = new HttpModel.Account(account)
        {
            Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId.Value })
        };

        var accountUrl = linkGenerator.GetAccountUrl(httpContext, account.AccountId);
        if(payload.OnlyReturnExisting)
        {
            httpContext.AddLocationResponseHeader(accountUrl);
            return Results.Ok(accountResponse);
        }

        return Results.Created(accountUrl, accountResponse);
    }


    public static async Task<IResult> GetOrSetAccount(
        string accountId, 
        HttpContext httpContext, 
        IAccountService accountService, 
        LinkGenerator linkGenerator)
    {
        var requestAccountId = httpContext.User.GetAccountId();
        if (requestAccountId != new AccountId(accountId))
        {
            return Results.Unauthorized();
        }

        var acmeRequest = httpContext.GetAcmeRequest();
        
        Model.Account? account;
        if (!acmeRequest.TryGetPayload<UpdateAccount>(out var payload))
        {
            account = await accountService.LoadAcountAsync(requestAccountId, httpContext.RequestAborted);

            if (account is null)
            {
                return Results.NotFound();
            }
        }
        else
        {
            account = await accountService.UpdateAccountAsync(requestAccountId, payload, httpContext.RequestAborted);
        }


        var accountResponse = new HttpModel.Account(account)
        {
            Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId.Value })
        };

        return Results.Ok(accountResponse);
    }

    public static async Task<IResult> ChangeAccountKey(
        HttpContext httpContext,
        IAccountService accountService,
        LinkGenerator linkGenerator)
    {
        var outerJws = httpContext.GetAcmeRequest();
        if (outerJws.Payload is null)
        {
            throw AcmeErrors.MalformedRequest("Outer JWS payload was empty.").AsException();
        }

        var innerJws = JsonSerializer.Deserialize<AcmeJwsToken>(Base64UrlEncoder.Decode(outerJws.Payload));
        if (innerJws is null)
        {
            throw AcmeErrors.MalformedRequest("Inner JWS was empty or could not be read.").AsException();
        }

        if (!innerJws.TryGetPayload<ChangeAccountKey>(out var payload) || payload is null)
        {
            throw AcmeErrors.MalformedRequest("Payload was empty or could not be read.").AsException();
        }

        var account = await accountService.ChangeAccountKeyAsync(
            httpContext.User.GetAccountId(), 
            outerJws,
            innerJws, 
            payload,
            httpContext.RequestAborted);

        var response = new HttpModel.Account(account)
        {
            Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId.Value })
        };
            
        return Results.Ok(response);
    }


    public static async Task<IResult> GetOrdersList(
        string accountId, 
        HttpContext httpContext, 
        IAccountService accountService,
        LinkGenerator linkGenerator)
    {
        var requestAccountId = httpContext.User.GetAccountId();
        if (requestAccountId != new AccountId(accountId))
        {
            return Results.Unauthorized();
        }

        var orderIds = await accountService.GetOrderIdsAsync(requestAccountId, httpContext.RequestAborted);
        var orderUrls = orderIds
            .Select(x => linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrder, new { orderId = x.Value }));

        return Results.Ok(new OrdersList(orderUrls!));
    }
}
