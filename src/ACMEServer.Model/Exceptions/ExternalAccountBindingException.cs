namespace Th11s.ACMEServer.Model.Exceptions
{
    public class ExternalAccountBindingRequiredException : AcmeException
    {
        private const string Detail = "External account binding is required";

        public ExternalAccountBindingRequiredException() : base(Detail) { }

        public override string ErrorType => "externalAccountRequired";
    }

    public class  ExternalAccountBindingFailedException : CustomAcmeException
    {
        public ExternalAccountBindingFailedException(string detailMessage) 
            : base(detailMessage) { }

        public override string ErrorType => "externalAccountInvalid";
    }
}
