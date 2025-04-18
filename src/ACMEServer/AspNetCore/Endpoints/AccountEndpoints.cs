using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TGIT.ACME.Protocol.HttpModel.Requests;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints
{
    public static class AccountEndpoints
    {
        public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder builder)
        {
            builder.MapPost("/new-account", CreateOrGetAccount)
                .RequireAuthorization()
                .WithName(EndpointNames.NewAccount);

            builder.MapMethods("/account/{accountId}", [HttpMethods.Post, HttpMethods.Put], SetAccount)
                .RequireAuthorization(Policies.ValidAccount)
                .WithName(EndpointNames.GetAccount);

            builder.MapPost("/account/{accountId}/orders", GetOrdersList)
                .RequireAuthorization(Policies.ValidAccount)
                .WithName(EndpointNames.GetOrderList);

            return builder;
        }


        public static async Task<IResult> CreateOrGetAccount(
            HttpContext httpContext,
            AcmeJwsHeader header, 
            IAccountService accountService,
            LinkGenerator linkGenerator,
            CancellationToken ct)
        {
            var acmeRequest = httpContext.GetAcmeRequest();
            if(!acmeRequest.TryGetPayload<CreateOrGetAccount>(out var payload) || payload is null)
                throw AcmeErrors.MalformedRequest("Payload was empty or could not be read.").AsException();

            Model.Account? account = null;
            if (payload.OnlyReturnExisting)
            {
                account = await accountService.FindAccountAsync(header.Jwk!, ct)
                    ?? throw AcmeErrors.AccountDoesNotExist().AsException();
            }
            else
            {
                account = await accountService.CreateAccountAsync(
                    header, //Post requests are validated, JWK exists.
                    payload.Contact,
                    payload.TermsOfServiceAgreed,
                    payload.ExternalAccountBinding,
                    ct);
            }

            var accountResponse = new HttpModel.Account(account)
            {
                Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId })
            };

            httpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetAccount, new { accountId = account.AccountId });
            var accountUrl = linkGenerator.GetUriByName(httpContext, EndpointNames.GetAccount, new { accountId = account.AccountId });

            return payload.OnlyReturnExisting
                ? Results.Ok(accountResponse)
                : Results.Created(accountUrl, accountResponse);
        }


        public static async Task<IResult> SetAccount(string accountId, AcmePayload<UpdateAccount> payload, IAccountService accountService, HttpContext httpContext, LinkGenerator linkGenerator, CancellationToken ct)
        {
            var account = await accountService.FromRequestAsync(ct);
            if (account.AccountId != accountId)
                return Results.Unauthorized();

            Model.AccountStatus? status = null;
            if (payload.Value.Status != null)
            {
                status = Enum.Parse<Model.AccountStatus>(payload.Value.Status, ignoreCase: true);
            }

            account = await accountService.UpdateAccountAsync(account, payload.Value.Contact, status, payload.Value.TermsOfServiceAgreed, ct);

            var accountResponse = new HttpModel.Account(account)
            {
                Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId })
            };

            return Results.Ok(accountResponse);
        }

        public static async Task<IResult> GetOrdersList(
            string accountId, 
            AcmePayload<object> payload, 
            IAccountService accountService,
            HttpContext httpContext, 
            LinkGenerator linkGenerator,
            CancellationToken ct)
        {
            var account = await accountService.FromRequestAsync(ct);
            if (account.AccountId != accountId)
                return Results.Unauthorized();

            var orderIds = await accountService.GetOrderIdsAsync(account, ct);
            var orderUrls = orderIds
                .Select(x => linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrder, new { orderId = x }));

            return Results.Ok(new OrdersList(orderUrls!));
        }
    }
}
