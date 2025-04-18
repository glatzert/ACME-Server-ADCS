namespace Th11s.ACMEServer.Model.Exceptions;

public class AcmeBaseException : Exception
{
    public AcmeBaseException(string message) : base(message) { }
}

public abstract class AcmeException : AcmeBaseException
{
    protected AcmeException(string message)
        : base(message) { }

    public string UrnBase { get; protected set; } = "urn:ietf:params:acme:error";
    public abstract string ErrorType { get; }
}

public class AcmeErrorException : AcmeBaseException
{
    public AcmeErrorException(AcmeError error) 
        : base($"{error.Type} - {error.Detail}")
    {
        Error = error;
    }

    public AcmeError Error { get; }
}
