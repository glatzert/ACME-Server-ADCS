using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Linq;
using TGIT.ACME.Server.Extensions;

namespace TGIT.ACME.Server.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AcmeLocationAttribute : Attribute, IFilterMetadata
    {
        public AcmeLocationAttribute(string routeName)
        {
            RouteName = routeName;
        }

        public string RouteName { get; }
    }

    public class AcmeLocationFilter : IActionFilter
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        public AcmeLocationFilter(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        public void OnActionExecuted(ActionExecutedContext context) 
        {
            var locationAttribute = context.ActionDescriptor.FilterDescriptors
                    .Select(x => x.Filter)
                    .OfType<AcmeLocationAttribute>()
                    .FirstOrDefault();

            if (locationAttribute == null)
                return;

            var urlHelper = _urlHelperFactory.GetUrlHelper(context);

            var locationHeaderUrl = urlHelper.RouteUrl(locationAttribute.RouteName, context.RouteData.Values, context.HttpContext.GetProtocol());
            var locationHeader = $"{locationHeaderUrl}";

            context.HttpContext.Response.Headers.Add("Location", locationHeader);
        }

        public void OnActionExecuting(ActionExecutingContext context)
        { }
    }
}
