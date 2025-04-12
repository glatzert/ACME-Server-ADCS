using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.HttpModel.Requests;

namespace Th11s.ACMEServer.AspNetCore.ModelBinding
{
    public class AcmePayloadBinder<TPayload> : IModelBinder
        where TPayload : new()
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };


        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var jwsToken = bindingContext.HttpContext.GetAcmeRequest();
            if (jwsToken is not null)
            {
                var acmePayload = jwsToken.AcmePayload.Deserialize<TPayload>(_jsonOptions);
                bindingContext.Result = ModelBindingResult.Success(new AcmePayload<TPayload>(acmePayload ?? new TPayload()));
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }
    }
}
