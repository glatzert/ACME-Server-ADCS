using Th11s.ACMEServer.HttpModel.Requests;

namespace Th11s.ACMEServer.HttpModel.Services
{
    public interface IAcmeRequestProvider
    {
        void Initialize(AcmeRawPostRequest rawPostRequest);

        AcmeRawPostRequest GetRequest();

        AcmeHeader GetHeader();

        TPayload GetPayload<TPayload>();
    }
}
