using Microsoft.Extensions.Hosting;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.HostedServices;

public class HostedOrderValidationService : BackgroundService
{
    private readonly OrderValidationProcessor _orderValidationProcessor;

    public HostedOrderValidationService(OrderValidationProcessor processor)
    {
        _orderValidationProcessor = processor;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _orderValidationProcessor.ProcessOrdersAsync(stoppingToken);
    }
}
