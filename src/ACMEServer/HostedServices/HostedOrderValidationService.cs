using Microsoft.Extensions.Hosting;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.HostedServices;

public class HostedOrderValidationService(OrderValidationProcessor processor) : BackgroundService
{
    private readonly OrderValidationProcessor _orderValidationProcessor = processor;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _orderValidationProcessor.ProcessOrdersAsync(stoppingToken);
    }
}
