using System;

namespace TGIT.ACME.Protocol.Model.Exceptions
{
    public class ConcurrencyException : InvalidOperationException
    {
        public ConcurrencyException()
            : base($"Object has been changed since loading")
        { }
    }
}
