using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Th11s.ACMEServer.AspNetCore.Filters;

namespace Th11s.ACMEServer.AspNetCore.Controllers
{
    [ApiController]
    [AddNextNonce]
    public class NonceController : ControllerBase
    {
        [Route("/new-nonce", Name = "NewNonce")]
        [HttpGet, HttpHead]
        public ActionResult GetNewNonce()
        {
            if (HttpMethods.IsGet(HttpContext.Request.Method))
                return NoContent();
            else
                return Ok();
        }
    }
}
