using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Th11s.ACMEServer.HttpModel.Services;

namespace Th11s.ACMEServer.AspNetCore.Filters
{
    public class ValidateAcmeRequestFilter : IAsyncActionFilter
    {
        private readonly IAcmeRequestProvider _requestProvider;
        private readonly IRequestValidationService _validationService;

        public ValidateAcmeRequestFilter(IAcmeRequestProvider requestProvider, IRequestValidationService validationService)
        {
            _requestProvider = requestProvider;
            _validationService = validationService;
        }


        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (HttpMethods.IsPost(context.HttpContext.Request.Method))
            {
                var acmeRequest = _requestProvider.GetRequest();
                var acmeHeader = _requestProvider.GetHeader();
                await _validationService.ValidateRequestAsync(acmeRequest, acmeHeader,
                    context.HttpContext.Request.GetDisplayUrl(), context.HttpContext.RequestAborted);
            }

            await next();
        }
    }
}
