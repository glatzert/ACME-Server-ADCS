namespace Th11s.ACMEServer.Model.Exceptions;

public class AcmeBaseException(string message) 
    : Exception(message)
{ }

public abstract class AcmeException(string message) 
    : AcmeBaseException(message)
{
    public string UrnBase { get; protected set; } = "urn:ietf:params:acme:error";
    public abstract string ErrorType { get; }
}

public class AcmeErrorException(AcmeError error) 
    : AcmeBaseException($"{error.Type} - {error.Detail}")
{
    public AcmeError Error { get; } = error;
}
