using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.RequestServices;

namespace TGIT.ACME.Server.ModelBinding
{
    public class AcmeHeaderBinder : IModelBinder
    {
        private readonly IAcmeRequestProvider _requestProvider;

        public AcmeHeaderBinder(IAcmeRequestProvider requestProvider)
        {
            _requestProvider = requestProvider;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext is null)
                throw new ArgumentNullException(nameof(bindingContext));

            var acmeHeader = _requestProvider.GetHeader();
            bindingContext.Result = ModelBindingResult.Success(acmeHeader);

            return Task.CompletedTask;
        }
    }
}
