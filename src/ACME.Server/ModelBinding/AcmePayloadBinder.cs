using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.HttpModel.Requests;
using TGIT.ACME.Protocol.RequestServices;

namespace TGIT.ACME.Server.ModelBinding
{
    public class AcmePayloadBinder<TPayload> : IModelBinder
    {
        private readonly IAcmeRequestProvider _requestProvider;

        public AcmePayloadBinder(IAcmeRequestProvider requestProvider)
        {
            _requestProvider = requestProvider;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext is null)
                throw new ArgumentNullException(nameof(bindingContext));

            var acmePayload = new AcmePayload<TPayload>(_requestProvider.GetPayload<TPayload>());
            bindingContext.Result = ModelBindingResult.Success(acmePayload);

            return Task.CompletedTask;
        }
    }
}
