using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.AspNetCore.Filters;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.HttpModel.Requests;
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
        public Task<ActionResult<Account>> CreateOrGetAccount(AcmeHeader header, AcmePayload<CreateOrGetAccount> payload)
        {
            if (payload.Value.OnlyReturnExisting)
                return FindAccountAsync(header);

            return CreateAccountAsync(header, payload);
        }

        private async Task<ActionResult<Account>> CreateAccountAsync(AcmeHeader header, AcmePayload<CreateOrGetAccount> payload)
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

        private async Task<ActionResult<Account>> FindAccountAsync(AcmeHeader header)
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
        public Task<ActionResult<Account>> SetAccount(string accountId)
        {
            throw new NotImplementedException();
        }

        [Route("/account/{accountId}/orders", Name = "OrderList")]
        [HttpPost]
        public Task<ActionResult<OrdersList>> GetOrdersList(string accountId, AcmePayload<object> payload)
        {
            throw new NotImplementedException();
        }
    }
}
