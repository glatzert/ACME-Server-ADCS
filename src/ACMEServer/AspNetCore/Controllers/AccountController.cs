using Microsoft.AspNetCore.Mvc;
using TGIT.ACME.Protocol.HttpModel.Requests;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.AspNetCore.Filters;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.HttpModel.Requests.JWS;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.AspNetCore.Controllers
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
        public Task<ActionResult<Account>> CreateOrGetAccount(AcmeJwsHeader header, AcmePayload<CreateOrGetAccount> payload)
        {
            if (payload.Value.OnlyReturnExisting)
                return FindAccountAsync(header);

            return CreateAccountAsync(header, payload);
        }

        private async Task<ActionResult<Account>> CreateAccountAsync(AcmeJwsHeader header, AcmePayload<CreateOrGetAccount> payload)
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
            var accountResponse = new HttpModel.Account(account, ordersUrl);

            var accountUrl = Url.RouteUrl("Account", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            return new CreatedResult(accountUrl, accountResponse);
        }

        private async Task<ActionResult<Account>> FindAccountAsync(AcmeJwsHeader header)
        {
            var account = await _accountService.FindAccountAsync(header.Jwk!, HttpContext.RequestAborted);

            if (account == null)
                throw new AccountNotFoundException();

            RouteData.Values.Add("accountId", account.AccountId);
            var ordersUrl = Url.RouteUrl("OrderList", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            var accountResponse = new HttpModel.Account(account, ordersUrl);

            return Ok(accountResponse);
        }

        [Route("/account/{accountId}", Name = "Account")]
        [HttpPost, HttpPut]
        public async Task<ActionResult<Account>> SetAccount(string accountId, AcmePayload<UpdateAccount> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            if (account.AccountId != accountId)
                return Unauthorized();
            
            Model.AccountStatus? status = null;
            if (payload.Value.Status != null)
            {
                status = Enum.Parse<Model.AccountStatus>(payload.Value.Status, ignoreCase: true);
            }

            account = await _accountService.UpdateAccountAsync(account, payload.Value.Contact, status, payload.Value.TermsOfServiceAgreed, HttpContext.RequestAborted);

            var ordersUrl = Url.RouteUrl("OrderList", new { accountId = account.AccountId }, HttpContext.GetProtocol());
            var accountResponse = new Account(account, ordersUrl);

            return Ok(accountResponse);
        }

        [Route("/account/{accountId}/orders", Name = "OrderList")]
        [HttpPost]
        public async Task<ActionResult<OrdersList>> GetOrdersList(string accountId, AcmePayload<object> payload)
        {
            var account = await _accountService.FromRequestAsync(HttpContext.RequestAborted);
            if (account.AccountId != accountId)
                return Unauthorized();

            var orders = await _accountService.GetOrderIdsAsync(account, HttpContext.RequestAborted);

            var orderUrls = orders.Select(x => Url.RouteUrl("GetOrder", new { orderId = x }, HttpContext.GetProtocol()));

            return Ok(new OrdersList(orderUrls));
        }
    }
}
