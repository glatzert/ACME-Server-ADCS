namespace TGIT.ACME.Protocol.Model.Exceptions
{
    public class BadNonceException : AcmeException
    {
        private const string Detail = "The nonce could not be accepted.";

        public BadNonceException() : base(Detail) { }

        public override string ErrorType => "badNonce";
    }
}
