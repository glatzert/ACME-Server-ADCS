namespace TGIT.ACME.Protocol.HttpModel.Requests
{
    public class AcmePayload<TPayload>
    {
        public AcmePayload(TPayload value)
        {
            Value = value;
        }

        public TPayload Value { get; }
    }
}
