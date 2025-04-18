using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.Model;

public class Nonce
{
    private string? _token;

    private Nonce() { }

    public Nonce(string token)
    {
        Token = token;
    }

    public string Token
    {
        get => _token ?? throw new NotInitializedException();
        private set => _token = value;
    }
}
