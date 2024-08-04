namespace Th11s.ACMEServer.Model.Services
{
    public interface INonceService
    {
        Task<Nonce> CreateNonceAsync(CancellationToken cancellationToken);

    }
}
