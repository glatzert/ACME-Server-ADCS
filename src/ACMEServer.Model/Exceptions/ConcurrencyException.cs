namespace Th11s.ACMEServer.Model.Exceptions
{
    public class ConcurrencyException : InvalidOperationException
    {
        public ConcurrencyException()
            : base($"Object has been changed since loading")
        { }
    }
}
