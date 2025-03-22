using Microsoft.AspNetCore.Mvc.ModelBinding;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.HttpModel.Services;

namespace Th11s.ACMEServer.AspNetCore.ModelBinding
{
    public class AcmeHeaderBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var acmeRequestWrapper = bindingContext.HttpContext.Features.Get<AcmeRequest>();
            if(acmeRequestWrapper is null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(acmeRequestWrapper.Request.AcmeHeader);
            return Task.CompletedTask;
        }
    }
}
