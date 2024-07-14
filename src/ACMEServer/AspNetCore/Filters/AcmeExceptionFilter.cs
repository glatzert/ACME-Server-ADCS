using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.AspNetCore.Filters
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
                    result = new ConflictObjectResult(new Protocol.HttpModel.AcmeError(acmeException));
                else if (acmeException is NotAllowedException)
                    result = new UnauthorizedObjectResult(new Protocol.HttpModel.AcmeError(acmeException));
                else if (acmeException is NotFoundException)
                    result = new NotFoundObjectResult(new Protocol.HttpModel.AcmeError(acmeException));
                else
                    result = new BadRequestObjectResult(new Protocol.HttpModel.AcmeError(acmeException));

                result.ContentTypes.Add("application/problem+json");
                context.Result = result;
            }
        }
    }
}
