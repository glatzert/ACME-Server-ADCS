using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.InMemory;

public class InMemoryNonceStore : INonceStore
{
    private readonly HashSet<string> _nonces = [];

    public Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken)
    {
        _nonces.Add(nonce.Token);
        return Task.CompletedTask;
    }

    public Task<bool> TryRemoveNonceAsync(Nonce nonce, CancellationToken cancellationToken) 
        => Task.FromResult(_nonces.Remove(nonce.Token));
}
