using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;
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
            if (context.Exception is AcmeBaseException acmeBaseException)
            {
                _logger.LogDebug(context.Exception, $"Detected {acmeBaseException.GetType()}. Converting to BadRequest.");
#if DEBUG
                _logger.LogError(context.Exception, "AcmeException detected.");
#endif

                if (acmeBaseException is AcmeErrorException aee)
                {
                    context.Result = new ObjectResult(new HttpModel.AcmeError(aee.Error))
                    {
                        StatusCode = aee.Error.HttpStatusCode ?? (int)HttpStatusCode.BadRequest
                    };
                }


                else if (acmeBaseException is AcmeException acmeException)
                {

                    if (acmeException is ConflictRequestException)
                        context.Result = new ConflictObjectResult(new HttpModel.AcmeError(acmeException));
                    else if (acmeException is NotAllowedException)
                        context.Result = new UnauthorizedObjectResult(new HttpModel.AcmeError(acmeException));
                    else if (acmeException is NotFoundException)
                        context.Result = new NotFoundObjectResult(new HttpModel.AcmeError(acmeException));
                    else
                        context.Result = new BadRequestObjectResult(new HttpModel.AcmeError(acmeException));
                }

                ((ObjectResult)context.Result!).ContentTypes.Add("application/problem+json");
                context.ExceptionHandled = true;
            }
        }
    }
}
