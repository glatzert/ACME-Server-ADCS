using ACMEServer.Storage.InMemory;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Tests.Utils;

namespace Th11s.ACMEServer.Tests.Services;

public class RenewalInfoServiceTests
{
    private readonly InMemoryCertificateStore _certificateStore;
    private readonly InMemoryOrderStore _orderStore;

    private readonly IOptions<ACMEServerOptions> _ariOptions;

    public RenewalInfoServiceTests()
    {
        _certificateStore = new InMemoryCertificateStore();
        _orderStore = new InMemoryOrderStore();

        _ariOptions = new OptionsWrapper<ACMEServerOptions>(new()
        {
            ARI = new()
            {
                PlannedRenewals =
                {
                    new()
                    {
                        ProfileName = null,
                        // TODO ...
                    },
                    new()
                    {
                        ProfileName = "test-profile",
                        // TODO ...
                    }
                }
            }
        });

        var csrBuilder = new CertificateRequestBuilder();
        csrBuilder.WithDnsName("ari-test.th11s.it");

        var csr = csrBuilder.AsBase64Url();

        var rootCertificate = X509SelfSignedCertificateUtility.CreateFakeRootCertificate();

        var certificate1 = X509SelfSignedCertificateUtility.CreateCertificateSignedByRoot(rootCertificate, csr, DateTimeOffset.UtcNow.AddMonths(-6));
        var certificate2 = X509SelfSignedCertificateUtility.CreateCertificateSignedByRoot(rootCertificate, csr, DateTimeOffset.UtcNow.AddDays(-10));

        
    }

    [Fact]
    public Task Default_ARI_Information_Will_Be_Returned()
    {
        var sut = new DefaultRenewalInfoService(_certificateStore, _orderStore, _ariOptions, NullLogger<DefaultRenewalInfoService>.Instance);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Planned_ARI_Information_Will_Be_Returned()
    {
        var sut = new DefaultRenewalInfoService(_certificateStore, _orderStore, _ariOptions, NullLogger<DefaultRenewalInfoService>.Instance);

        return Task.CompletedTask;
    }
}
