namespace Th11s.ACMEServer.Model.Exceptions
{
    public class ExternalAccountBindingFailedException : CustomAcmeException
    {
        public ExternalAccountBindingFailedException(string detailMessage) 
            : base(detailMessage) { }

        public override string ErrorType => "externalAccountInvalid";
    }
}
