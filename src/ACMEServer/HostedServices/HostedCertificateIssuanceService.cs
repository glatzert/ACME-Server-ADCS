using Microsoft.Extensions.Hosting;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.HostedServices;

public class HostedCertificateIssuanceService : BackgroundService
{
    private readonly CertificateIssuanceProcessor _certificateIssuanceProcessor;

    public HostedCertificateIssuanceService(CertificateIssuanceProcessor processor)
    {
        _certificateIssuanceProcessor = processor;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _certificateIssuanceProcessor.ProcessCertificatesAsync(stoppingToken);
    }
}
