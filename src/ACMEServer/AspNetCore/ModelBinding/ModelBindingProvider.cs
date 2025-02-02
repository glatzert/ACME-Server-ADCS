using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.HttpModel.Requests.JWS;

namespace Th11s.ACMEServer.AspNetCore.ModelBinding
{
    public class AcmeModelBindingProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            var modelType = context.Metadata.ModelType;
            if (modelType == typeof(AcmeJwsHeader))
                return new BinderTypeModelBinder(typeof(AcmeHeaderBinder));

            if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(AcmePayload<>))
            {
                var type = typeof(AcmePayloadBinder<>).MakeGenericType(modelType.GetGenericArguments());
                return new BinderTypeModelBinder(type);
            }

            return null;
        }
    }
}
