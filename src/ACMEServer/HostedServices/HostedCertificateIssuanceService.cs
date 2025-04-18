using Microsoft.Extensions.Hosting;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.HostedServices;

public class HostedCertificateIssuanceService(CertificateIssuanceProcessor processor) : BackgroundService
{
    private readonly CertificateIssuanceProcessor _certificateIssuanceProcessor = processor;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _certificateIssuanceProcessor.ProcessCertificatesAsync(stoppingToken);
    }
}
