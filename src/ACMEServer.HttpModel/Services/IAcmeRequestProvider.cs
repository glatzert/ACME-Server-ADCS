using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.HttpModel.Services
{
    public interface IAcmeRequestProvider
    {
        void Initialize(AcmeJwsToken rawPostRequest);

        AcmeJwsToken GetRequest();

        AcmeJwsHeader GetHeader();

        TPayload GetPayload<TPayload>();
    }
}
