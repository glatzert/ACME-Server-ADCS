using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Storage;

namespace TGIT.ACME.Protocol.Services
{
    public class DefaultNonceService : INonceService
    {
        private readonly INonceStore _nonceStore;
        private readonly ILogger<DefaultNonceService> _logger;

        public DefaultNonceService(INonceStore nonceStore, ILogger<DefaultNonceService> logger)
        {
            _nonceStore = nonceStore;
            _logger = logger;
        }

        public  async Task<Nonce> CreateNonceAsync(CancellationToken cancellationToken)
        {
            var nonce = new Nonce(GuidString.NewValue());

            await _nonceStore.SaveNonceAsync(nonce, cancellationToken);
            _logger.LogInformation($"Created and saved new nonce: {nonce.Token}.");

            return nonce;
        }
    }
}
