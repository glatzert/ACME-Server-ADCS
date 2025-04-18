using System.Runtime.CompilerServices;

namespace Th11s.ACMEServer.Model.Exceptions;

public class NotInitializedException([CallerMemberName] string caller = null!) 
    : InvalidOperationException($"{caller} has been accessed before being initialized.")
{ }
