using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using Th11s.ACMEServer.AspNetCore.Middleware;
using Th11s.ACMEServer.HttpModel.Requests;

namespace Th11s.ACMEServer.AspNetCore.ModelBinding
{
    public class AcmePayloadBinder<TPayload> : IModelBinder
        where TPayload : new()
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);
            
            var acmeRequest = bindingContext.HttpContext.Features.Get<AcmeRequest>();
            if (acmeRequest?.Request?.Payload == null)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            var acmePayload = JsonSerializer.Deserialize<TPayload>(acmeRequest.Request.Payload);
            bindingContext.Result = ModelBindingResult.Success(new AcmePayload<TPayload>(acmePayload ?? new TPayload()));

            return Task.CompletedTask;
        }
    }
}
