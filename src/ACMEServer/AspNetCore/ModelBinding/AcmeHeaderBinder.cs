using Microsoft.AspNetCore.Mvc.ModelBinding;
using Th11s.ACMEServer.AspNetCore.Extensions;

namespace Th11s.ACMEServer.AspNetCore.ModelBinding
{
    public class AcmeHeaderBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var jwsToken = bindingContext.HttpContext.GetAcmeRequest();
            bindingContext.Result = jwsToken is not null 
                ? ModelBindingResult.Success(jwsToken.AcmeHeader) 
                : ModelBindingResult.Failed();

            return Task.CompletedTask;
        }
    }
}
