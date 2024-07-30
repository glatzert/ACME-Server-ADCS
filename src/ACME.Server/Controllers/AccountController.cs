using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.HttpModel.Requests;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Server.Extensions;
using TGIT.ACME.Server.Filters;

namespace TGIT.ACME.Server.Controllers
{
    [AddNextNonce]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [Route("/new-account", Name = "NewAccount")]
        [AcmeLocation("Account", "accountId")]
        [HttpPost]
        public Task<ActionResult<Protocol.HttpModel.Account>> CreateOrGetAccount(AcmeHeader header, AcmePayload<CreateOrGetAccount> payload)
        {
            if (payload.Value.OnlyReturnExisting)
                return FindAccountAsync(header);
            
            return CreateAccountAsync(header, payload);
        }

        private async Task<ActionResult<Protocol.HttpModel.Account>> CreateAccountAsync(AcmeHeader header, AcmePayload<CreateOrGetAccount> payload)
        {
            if (payload == null)
                throw new MalformedRequestException("Payload was empty or could not be read.");

            var account = await _accountService.CreateAccountAsync(
                header.Jwk!, //Post requests are validated, JWK exists.
                payload.Value.Contact,
                payload.Value.TermsOfServiceAgreed,
                HttpContext.RequestAborted);

            RouteData.Values.Add("accountId", account.AccountId);
            var ordersUrl = Url.RouteUrl("OrderList", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            var accountResponse = new Protocol.HttpModel.Account(account, ordersUrl);

            var accountUrl = Url.RouteUrl("Account", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            return new CreatedResult(accountUrl, accountResponse);
        }

        private async Task<ActionResult<Protocol.HttpModel.Account>> FindAccountAsync(AcmeHeader header)
        {
            var account = await _accountService.FindAccountAsync(header.Jwk!, HttpContext.RequestAborted);

            if (account == null)
                throw new AccountNotFoundException();

            RouteData.Values.Add("accountId", account.AccountId);
            var ordersUrl = Url.RouteUrl("OrderList", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            var accountResponse = new Protocol.HttpModel.Account(account, ordersUrl);

            return Ok(accountResponse);
        }

        [Route("/account/{accountId}", Name = "Account")]
        [HttpPost, HttpPut]
        public async Task<ActionResult<Protocol.HttpModel.Account>> SetAccount(string accountId, AcmePayload<UpdateAccount> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            
            AccountStatus? status = null;
            if (payload.Value.Status != null)
            {
                status = Enum.Parse<AccountStatus>(payload.Value.Status, true);
                if(status != AccountStatus.Deactivated)
                    throw new MalformedRequestException("Only 'deactivated' status is allowed to be set.");
            }

            await _accountService.UpdateAccountAsync(account, payload.Value.Contact, status, HttpContext.RequestAborted);

            var ordersUrl = Url.RouteUrl("OrderList", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            var accountResponse = new Protocol.HttpModel.Account(account, ordersUrl);

            return Ok(accountResponse);
        }

        [Route("/account/{accountId}/orders", Name = "OrderList")]
        [HttpPost]
        public async Task<ActionResult<Protocol.HttpModel.OrdersList>> GetOrdersList(string accountId, AcmePayload<object> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            var orders = await _accountService.GetOrderIdsAsync(account, HttpContext.RequestAborted);

            var orderUrls = orders.Select(x => Url.RouteUrl("GetOrder", new { orderId = x }, HttpContext.GetProtocol()));

            return Ok(new Protocol.HttpModel.OrdersList(orderUrls));
        }
    }
}
