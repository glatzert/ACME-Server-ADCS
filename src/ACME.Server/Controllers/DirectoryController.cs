using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TGIT.ACME.Server.Configuration;
using TGIT.ACME.Server.Extensions;

namespace TGIT.ACME.Server.Controllers
{
    public class DirectoryController : ControllerBase
    {
        private readonly IOptions<ACMEServerOptions> _options;

        public DirectoryController(IOptions<ACMEServerOptions> options)
        {
            _options = options;
        }

        [Route("/", Name = "Directory")]
        public ActionResult<Protocol.HttpModel.Directory> GetDirectory()
        {
            var options = _options.Value;

            return new Protocol.HttpModel.Directory
            {
                NewNonce = Url.RouteUrl("NewNonce", null, HttpContext.GetProtocol()),
                NewAccount = Url.RouteUrl("NewAccount", null, HttpContext.GetProtocol()),
                NewOrder = Url.RouteUrl("NewOrder", null, HttpContext.GetProtocol()),
                NewAuthz = null,
                RevokeCert = null,
                KeyChange = Url.RouteUrl("KeyChange", null, HttpContext.GetProtocol()),

                Meta = new Protocol.HttpModel.DirectoryMetadata
                {
                    ExternalAccountRequired = false,
                    CAAIdentities = null,
                    TermsOfService = options.TOS.RequireAgreement ? options.TOS.Url : null,
                    Website = options.WebsiteUrl
                }
            };
        }
    }
}
