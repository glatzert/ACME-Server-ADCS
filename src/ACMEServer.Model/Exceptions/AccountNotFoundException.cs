namespace Th11s.ACMEServer.Model.Exceptions
{
    public class AccountNotFoundException : AcmeException
    {
        private const string Detail = "The account could not be found";

        public AccountNotFoundException() : base(Detail) { }

        public override string ErrorType => "accountDoesNotExist";
    }
}
