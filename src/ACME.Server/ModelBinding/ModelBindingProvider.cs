using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using TGIT.ACME.Protocol.HttpModel.Requests;

namespace TGIT.ACME.Server.ModelBinding
{
    public class AcmeModelBindingProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            var modelType = context.Metadata.ModelType;
            if (modelType == typeof(AcmeHeader))
                return new BinderTypeModelBinder(typeof(AcmeHeaderBinder));

            if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(AcmePayload<>)) {
                var type = typeof(AcmePayloadBinder<>).MakeGenericType(modelType.GetGenericArguments());
                return new BinderTypeModelBinder(type);
            }

            return null;
        }
    }
}
