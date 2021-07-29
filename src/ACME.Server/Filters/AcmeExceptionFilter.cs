using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using TGIT.ACME.Protocol.Model.Exceptions;

namespace TGIT.ACME.Server.Filters
{
    public class AcmeExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<AcmeExceptionFilter> _logger;

        public AcmeExceptionFilter(ILogger<AcmeExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is AcmeException acmeException)
            {
                _logger.LogDebug($"Detected {acmeException.GetType()}. Converting to BadRequest.");
#if DEBUG
                _logger.LogError(context.Exception, "AcmeException detected.");
#endif

                ObjectResult result;
                if (acmeException is ConflictRequestException)
                    result = new ConflictObjectResult(acmeException.GetHttpError());
                else if (acmeException is NotAllowedException)
                    result = new UnauthorizedObjectResult(acmeException.GetHttpError());
                else if (acmeException is NotFoundException)
                    result = new NotFoundObjectResult(acmeException.GetHttpError());
                else
                    result = new BadRequestObjectResult(acmeException.GetHttpError());
                
                result.ContentTypes.Add("application/problem+json");
                context.Result = result;
            }
        }
    }
}
