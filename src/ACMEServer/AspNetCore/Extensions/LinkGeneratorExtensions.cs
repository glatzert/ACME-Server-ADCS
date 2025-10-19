using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Th11s.ACMEServer.AspNetCore.Endpoints;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.AspNetCore.Extensions;

public static class LinkGeneratorExtensions
{
    public static string GetAccountUrl(this LinkGenerator linkGenerator, HttpContext httpContext, AccountId accountId)
        => linkGenerator.GetUriByName(httpContext, EndpointNames.GetAccount, new { accountId = accountId.Value })
            ?? throw new InvalidOperationException("Link generation failed for 'GetAccountUrl'");
}
