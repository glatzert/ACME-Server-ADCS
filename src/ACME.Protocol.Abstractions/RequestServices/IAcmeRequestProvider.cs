using TGIT.ACME.Protocol.HttpModel.Requests;

namespace TGIT.ACME.Protocol.RequestServices
{
    public interface IAcmeRequestProvider
    {
        void Initialize(AcmeRawPostRequest rawPostRequest);

        AcmeRawPostRequest GetRequest();

        AcmeHeader GetHeader();
        
        TPayload GetPayload<TPayload>();
    }
}
