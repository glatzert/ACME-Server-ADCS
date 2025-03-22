using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text.Json;
using Th11s.ACMEServer.Model.Features;

namespace Th11s.ACMEServer.HttpModel.Requests
{
    public class AcmePayload<TPayload>
    {
        public AcmePayload(TPayload value)
        {
            Value = value;
        }

        public TPayload Value { get; }


        public static ValueTask<AcmePayload<TPayload>> BindAsync(HttpContext httpContext, ParameterInfo parameterInfo)
        {
            var payload = httpContext.Features.Get<AcmeRequestFeature>()?.Request.AcmePayload ?? throw new InvalidOperationException();
            return ValueTask.FromResult(new AcmePayload<TPayload>(payload.Deserialize<TPayload>()));
        }
    }
}
