using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using TGIT.ACME.Server.Extensions;

namespace Th11s.ACMEServer.AspNetCore.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AcmeLocationAttribute : Attribute, IFilterMetadata
    {
        public AcmeLocationAttribute(string routeName, params string[] routeDataTokens)
        {
            RouteName = routeName;
            RouteDataTokens = routeDataTokens;
        }

        public string RouteName { get; }
        public string[] RouteDataTokens { get; }
    }

    public class AcmeLocationFilter : IActionFilter
    {
        private readonly LinkGenerator _linkGenerator;

        public AcmeLocationFilter(LinkGenerator linkGenerator)
        {
            _linkGenerator = linkGenerator;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var locationAttribute = context.ActionDescriptor.FilterDescriptors
                .Select(x => x.Filter)
                .OfType<AcmeLocationAttribute>()
                .FirstOrDefault();

            if (locationAttribute == null)
                return;

            var routeData = context.RouteData.Values
                .Where(x => locationAttribute.RouteDataTokens.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(x => x.Key, x => x.Value);

            var locationHeaderUrl = _linkGenerator.GetUriByRouteValues(context.HttpContext, locationAttribute.RouteName, routeData);

            context.HttpContext.Response.Headers.Add("Location", locationHeaderUrl);
        }

        public void OnActionExecuting(ActionExecutingContext context)
        { }
    }
}
