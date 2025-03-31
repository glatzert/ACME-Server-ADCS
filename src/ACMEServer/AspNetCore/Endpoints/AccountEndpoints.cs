using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TGIT.ACME.Protocol.HttpModel.Requests;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.HttpModel.Requests;
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
                .WithName(EndpointNames.NewAccount);

            builder.MapMethods("/account/{accountId}", [HttpMethods.Post, HttpMethods.Put], SetAccount)
                .WithName(EndpointNames.GetAccount);

            builder.MapPost("/account/{accountId}/orders", GetOrdersList)
                .WithName(EndpointNames.GetOrderList);

            return builder;
        }


        public static Task<IResult> CreateOrGetAccount(
            AcmeJwsHeader header, 
            AcmePayload<CreateOrGetAccount> payload, 
            IAccountService accountService,
            HttpContext httpContext,
            LinkGenerator linkGenerator,
            CancellationToken ct)
        {
            if (payload.Value.OnlyReturnExisting)
                return FindAccountAsync(header, accountService, httpContext, linkGenerator, ct);

            return CreateAccountAsync(header, payload, accountService, httpContext, linkGenerator, ct);
        }

        private static async Task<IResult> CreateAccountAsync(AcmeJwsHeader header, AcmePayload<CreateOrGetAccount> payload, IAccountService accountService, HttpContext httpContext, LinkGenerator linkGenerator, CancellationToken ct)
        {
            if (payload == null)
                throw new MalformedRequestException("Payload was empty or could not be read.");

            var account = await accountService.CreateAccountAsync(
                header, //Post requests are validated, JWK exists.
                payload.Value.Contact,
                payload.Value.TermsOfServiceAgreed,
                payload.Value.ExternalAccountBinding,
                ct);

            var accountResponse = new HttpModel.Account(account)
            {
                Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId })
            };

            httpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetAccount, new { accountId = account.AccountId });
            var accountUrl = linkGenerator.GetUriByName(httpContext, EndpointNames.GetAccount, new { accountId = account.AccountId });

            return Results.Created(accountUrl, accountResponse);
        }

        private static async Task<IResult> FindAccountAsync(AcmeJwsHeader header, IAccountService accountService, HttpContext httpContext, LinkGenerator linkGenerator, CancellationToken ct)
        {
            var account = await accountService.FindAccountAsync(header.Jwk!, ct)
                ?? throw Model.AcmeErrors.AccountDoesNotExist().AsException();

            var accountResponse = new HttpModel.Account(account)
            {
                Orders = linkGenerator.GetUriByName(httpContext, EndpointNames.GetOrderList, new { accountId = account.AccountId })
            };

            httpContext.AddLocationResponseHeader(linkGenerator, EndpointNames.GetAccount, new { accountId = account.AccountId });
            return Results.Ok(accountResponse);
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
