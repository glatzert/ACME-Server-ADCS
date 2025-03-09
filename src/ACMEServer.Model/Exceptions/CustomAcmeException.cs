namespace Th11s.ACMEServer.Model.Exceptions
{
    public abstract class CustomAcmeException : AcmeException
    {
        protected CustomAcmeException(string message)
            :base(message)
        {
            UrnBase = "urn:th11s:acme:error";
        }
    }
}
