using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.HttpModel.Services;

namespace Th11s.ACMEServer.AspNetCore.Filters
{
    public class ValidateAcmeRequestFilter : IAsyncActionFilter
    {
        private readonly IRequestValidationService _validationService;

        public ValidateAcmeRequestFilter(IRequestValidationService validationService)
        {
            _validationService = validationService;
        }


        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (HttpMethods.IsPost(context.HttpContext.Request.Method))
            {
                var acmeRequestWrapper = context.HttpContext.Features.Get<AcmeRequest>();

                var acmeRequest = acmeRequestWrapper.Request;
                var acmeHeader = acmeRequestWrapper.Request.AcmeHeader;
                await _validationService.ValidateRequestAsync(
                    acmeRequest, 
                    context.HttpContext.Request.GetDisplayUrl(), 
                    context.HttpContext.RequestAborted);
            }

            await next();
        }
    }
}
