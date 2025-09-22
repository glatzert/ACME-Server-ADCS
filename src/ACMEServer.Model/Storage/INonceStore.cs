namespace Th11s.ACMEServer.Model.Storage;

public interface INonceStore
{
    Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken);
    Task<bool> TryConsumeNonceAsync(Nonce nonce, CancellationToken cancellationToken);
}
