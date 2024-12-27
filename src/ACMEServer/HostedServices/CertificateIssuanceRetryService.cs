using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.HostedServices
{
    public class CertificateIssuanceRetryService : BackgroundService
    {
        private readonly Channel<OrderId> _queue;
        private readonly IOrderStore _orderStore;
        private readonly IOptions<ACMEServerOptions> _options;
        private readonly ILogger<CertificateIssuanceRetryService> _logger;

        public CertificateIssuanceRetryService(
            [FromKeyedServices(nameof(CertificateIssuanceProcessor))] Channel<OrderId> queue,
            IOrderStore orderStore,
            IOptions<ACMEServerOptions> options,
            ILogger<CertificateIssuanceRetryService> logger)
        {
            _queue = queue;
            _orderStore = orderStore;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(_options.Value.HostedWorkers!.IssuanceCheckInterval);
            using var periodic = new PeriodicTimer(interval);
            while (
                !stoppingToken.IsCancellationRequested &&
                await periodic.WaitForNextTickAsync(stoppingToken)
            )
            {
                try
                {
                    var orders = await _orderStore.GetFinalizableOrders(stoppingToken);
                    foreach (var order in orders)
                    {
                        _queue.Writer.TryWrite(new(order.OrderId));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing orders for issuance.");
                }
            }
        }
    }
}
