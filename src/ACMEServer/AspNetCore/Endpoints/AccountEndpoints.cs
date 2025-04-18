using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TGIT.ACME.Protocol.HttpModel.Requests;
using Th11s.ACMEServer.AspNetCore.Authorization;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Endpoints
{
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

            return builder;
        }


        public static async Task<IResult> CreateOrGetAccount(
            HttpContext httpContext,
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
                account = await accountService.FindAccountAsync(acmeRequest.AcmeHeader.Jwk!, ct)
                    ?? throw AcmeErrors.AccountDoesNotExist().AsException();
            }
            else
            {
                account = await accountService.CreateAccountAsync(
                    acmeRequest.AcmeHeader,
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


        public static async Task<IResult> GetOrSetAccount(
            string accountId, 
            HttpContext httpContext, 
            IAccountService accountService, 
            LinkGenerator linkGenerator, 
            CancellationToken ct)
        {
            var requesetAccountId = httpContext.User.GetAccountId();
            if (requesetAccountId != accountId)
            {
                return Results.Unauthorized();
            }

            var acmeRequest = httpContext.GetAcmeRequest();
            Model.Account? account = null;
            if (!acmeRequest.TryGetPayload<UpdateAccount>(out var payload))
            {
                account = await accountService.LoadAcountAsync(accountId, ct);

                if (account is null)
                {
                    return Results.NotFound();
                }
            }
            else
            {
                Model.AccountStatus? status = null;
                if (payload?.Status != null)
                {
                    status = Enum.Parse<Model.AccountStatus>(payload.Status, ignoreCase: true);
                }

                account = await accountService.UpdateAccountAsync(accountId, payload?.Contact, status, payload?.TermsOfServiceAgreed, ct);
            }


            var accountResponse = new HttpModel.Account(account)
            {
                Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId })
            };

            return Results.Ok(accountResponse);
        }

        public static async Task<IResult> GetOrdersList(
            string accountId, 
            HttpContext httpContext, 
            IAccountService accountService,
            LinkGenerator linkGenerator,
            CancellationToken ct)
        {
            var requesetAccountId = httpContext.User.GetAccountId();
            if (requesetAccountId != accountId)
            {
                return Results.Unauthorized();
            }

            var orderIds = await accountService.GetOrderIdsAsync(accountId, ct);
            var orderUrls = orderIds
                .Select(x => linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrder, new { orderId = x }));

            return Results.Ok(new OrdersList(orderUrls!));
        }
    }
}
