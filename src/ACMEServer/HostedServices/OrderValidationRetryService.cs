using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Storage;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.HostedServices;

public class OrderValidationRetryService(
    OrderValidationQueue queue,
    IServiceProvider serviceProvider,
    IOptions<ACMEServerOptions> options,
    ILogger<OrderValidationRetryService> logger) : BackgroundService
{
    private readonly OrderValidationQueue _queue = queue;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IOptions<ACMEServerOptions> _options = options;
    private readonly ILogger<OrderValidationRetryService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(_options.Value.HostedWorkers!.ValidationCheckInterval);
        using var periodic = new PeriodicTimer(interval);
        while (
            !stoppingToken.IsCancellationRequested &&
            await periodic.WaitForNextTickAsync(stoppingToken)
        )
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orderStore = scope.ServiceProvider.GetRequiredService<IOrderStore>();

                var orders = await orderStore.GetValidatableOrders(stoppingToken);
                foreach (var order in orders)
                {
                    _queue.Writer.TryWrite(order.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingOrdersForValidation(ex);
            }
        }
    }
}
