using System.Runtime.CompilerServices;

namespace Th11s.ACMEServer.Model.Exceptions
{
    public class NotInitializedException : InvalidOperationException
    {
        public NotInitializedException([CallerMemberName] string caller = null!)
            : base($"{caller} has been accessed before being initialized.")
        {

        }
    }
}
