using System;

namespace Th11s.ACMEServer.Model.Exceptions
{
    public abstract class AcmeException : Exception
    {
        protected AcmeException(string message)
            : base(message) { }

        public string UrnBase { get; protected set; } = "urn:ietf:params:acme:error";
        public abstract string ErrorType { get; }
    }
}
