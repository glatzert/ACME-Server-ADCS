namespace Th11s.ACMEServer.Model.Exceptions
{
    public class ExternalAccountBindingRequiredException : AcmeException
    {
        private const string Detail = "External account binding is required";

        public ExternalAccountBindingRequiredException() : base(Detail) { }

        public override string ErrorType => "externalAccountRequired";
    }
}
