using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Workers;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.HostedServices;

public class HostedOrderValidationService : BackgroundService
{
    private readonly OrderValidationProcessor _orderValidationProcessor;
    private readonly IOptions<ACMEServerOptions> _options;

    public HostedOrderValidationService(
        IOptions<ACMEServerOptions> options,
        OrderValidationProcessor orderValidationProcessor)
    {
        _orderValidationProcessor = orderValidationProcessor;
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _orderValidationProcessor.ProcessOrdersAsync(stoppingToken);
    }
}
