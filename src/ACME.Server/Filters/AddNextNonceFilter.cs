using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Services;

namespace TGIT.ACME.Server.Filters
{
    public class AddNextNonceAttribute : ServiceFilterAttribute
    {
        public AddNextNonceAttribute()
            : base(typeof(AddNextNonceFilter))
        { }
    }

    public class AddNextNonceFilter : IAsyncActionFilter, IAsyncExceptionFilter
    {
        private readonly INonceService _nonceService;
        private readonly ILogger<AddNextNonceFilter> _logger;

        public AddNextNonceFilter(INonceService nonceService, ILogger<AddNextNonceFilter> logger)
        {
            _nonceService = nonceService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next.Invoke();
            await AddNonceHeader(context.HttpContext);
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            await AddNonceHeader(context.HttpContext);
        }

        private async Task AddNonceHeader(HttpContext httpContext)
        {
            if (httpContext.Response.Headers.ContainsKey("Replay-Nonce"))
                return;

            var newNonce = await _nonceService.CreateNonceAsync(httpContext.RequestAborted);
            httpContext.Response.Headers.Add("Replay-Nonce", newNonce.Token);

            _logger.LogInformation($"Added Replay-Nonce: {newNonce.Token}");
        }

    }
}
