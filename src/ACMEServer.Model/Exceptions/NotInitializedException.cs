using System;
using System.Runtime.CompilerServices;

namespace TGIT.ACME.Protocol.Model.Exceptions
{
    public class NotInitializedException : InvalidOperationException
    {
        public NotInitializedException([CallerMemberName]string caller = null!)
            :base($"{caller} has been accessed before being initialized.")
        {

        }
    }
}
