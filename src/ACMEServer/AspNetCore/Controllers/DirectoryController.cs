using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.AspNetCore.Controllers
{
    public class DirectoryController : ControllerBase
    {
        private readonly IOptions<ACMEServerOptions> _options;

        public DirectoryController(IOptions<ACMEServerOptions> options)
        {
            _options = options;
        }

        [Route("/", Name = "Directory")]
        [Route("/directory", Name = "DirectoryAlt")]
        public ActionResult<HttpModel.Directory> GetDirectory()
        {
            var options = _options.Value;

            return new HttpModel.Directory
            {
                NewNonce = Url.RouteUrl("NewNonce", null, HttpContext.GetProtocol()),
                NewAccount = Url.RouteUrl("NewAccount", null, HttpContext.GetProtocol()),
                NewOrder = Url.RouteUrl("NewOrder", null, HttpContext.GetProtocol()),
                NewAuthz = null,
                RevokeCert = null,
                KeyChange = Url.RouteUrl("KeyChange", null, HttpContext.GetProtocol()),

                Meta = new HttpModel.DirectoryMetadata
                {
                    ExternalAccountRequired = _options.Value.ExternalAccountBinding?.Required == true,
                    CAAIdentities = null,
                    TermsOfService = options.TOS.RequireAgreement ? options.TOS.Url : null,
                    Website = options.WebsiteUrl
                }
            };
        }
    }
}
