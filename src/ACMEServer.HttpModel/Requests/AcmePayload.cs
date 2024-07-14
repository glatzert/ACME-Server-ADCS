namespace Th11s.ACMEServer.HttpModel.Requests
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
