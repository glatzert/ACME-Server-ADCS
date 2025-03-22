using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Th11s.ACMEServer.AspNetCore.Endpoints;

namespace Th11s.ACMEServer.AspNetCore.Filters
{
    public class AcmeIndexLinkFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context) { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var linkGenerator = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();

            var linkHeaderUrl = linkGenerator.GetUriByName(context.HttpContext, EndpointNames.Directory, null);
            var linkHeader = $"<{linkHeaderUrl}>;rel=\"index\"";

            context.HttpContext.Response.Headers.Append("Link", linkHeader);
        }
    }
}
