using System.Security.Cryptography.X509Certificates;

using ACMEServer.Storage.InMemory;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Tests.Utils;
using Th11s.ACMEServer.Tests.Utils.Fakes;

namespace Th11s.ACMEServer.Tests.Services;

public class RenewalInfoServiceTests
{
    [Fact]
    public async Task Default_ARI_Information_Will_Be_Returned()
    {
        var timeProvider = new FakeTimeProvider();

        var certificateStore = new InMemoryCertificateStore();
        var orderStore = new InMemoryOrderStore();

        int startDays = 15;
        var ariOptions = new OptionsWrapper<ACMEServerOptions>(new()
        {
            ARI = new()
            {
                DefaultRenewalStart = $"{startDays:0}.00:00:00",
            }
        });

        var csrBuilder = new CertificateRequestBuilder();
        csrBuilder.WithDnsName("ari-test.th11s.it");

        var csr = csrBuilder.AsBase64Url();

        var rootCertificate = X509SelfSignedCertificateUtility.CreateFakeRootCertificate(timeProvider);

        var certificate = X509SelfSignedCertificateUtility.CreateCertificateSignedByRoot(rootCertificate, csr, timeProvider.GetLocalNow().AddMonths(6));
        var certificateContainer = new CertificateContainer(
            new AccountId(),
            new OrderId(),
            new X509Certificate2Collection(certificate),
            new Dictionary<string, string>()
        );

        await certificateStore.SaveCertificateAsync(certificateContainer, TestContext.Current.CancellationToken);

        var sut = new DefaultRenewalInfoService(certificateStore, orderStore, ariOptions, NullLogger<DefaultRenewalInfoService>.Instance);
        var result = await sut.GetRenewalInformationAsync(certificateContainer.AccountId, certificateContainer.CertificateId, TestContext.Current.CancellationToken);

        Assert.NotNull(result);

        var expectedStart = certificate.NotAfter.AddDays(-startDays);
        var expectedEnd = expectedStart.Add(ariOptions.Value.ARI.DefaultRenewalPeriod);
        Assert.Equal(expectedStart.ToString("O"), result.SuggestedWindow.Start);
        Assert.Equal(expectedEnd.ToString("O"), result.SuggestedWindow.End);
    }

    [Fact]
    public async Task Planned_ARI_Information_Will_Be_Returned()
    {
        var timeProvider = new FakeTimeProvider();

        var certificateStore = new InMemoryCertificateStore();
        var orderStore = new InMemoryOrderStore();

        int startDays = 15;
        var renewalStart = timeProvider.GetLocalNow().AddMonths(7).AddDays(startDays);
        var ariOptions = new OptionsWrapper<ACMEServerOptions>(new()
        {
            ARI = new()
            {
                PlannedRenewals =
                {
                    new()
                    {
                        NotBeforeOlderThan = timeProvider.GetLocalNow().AddMonths(7),
                        ProfileName = "test-profile",
                        RenewalStart = renewalStart,
                        RenewalPeriod = TimeSpan.FromDays(2)
                    }
                }
            }
        });

        var csrBuilder = new CertificateRequestBuilder();
        csrBuilder.WithDnsName("ari-test.th11s.it");

        var csr = csrBuilder.AsBase64Url();

        var rootCertificate = X509SelfSignedCertificateUtility.CreateFakeRootCertificate(timeProvider);

        var certificate = X509SelfSignedCertificateUtility.CreateCertificateSignedByRoot(rootCertificate, csr, timeProvider.GetLocalNow().AddMonths(6));
        var certificateContainer = new CertificateContainer(
            new AccountId(),
            new OrderId(),
            new X509Certificate2Collection(certificate),
            new Dictionary<string, string>()
        );

        var order = new Order(
            certificateContainer.OrderId,
            certificateContainer.AccountId,
            OrderStatus.Valid,
            [new (IdentifierTypes.DNS, "ari-test.th11s.it")],
            [],
            null,
            null,
            null,
            new("test-profile"),
            null,
            certificateContainer.CertificateId,
            null,
            null,
            null,
            1
        );

        await orderStore.SaveOrderAsync(order, TestContext.Current.CancellationToken);
        await certificateStore.SaveCertificateAsync(certificateContainer, TestContext.Current.CancellationToken);

        var sut = new DefaultRenewalInfoService(certificateStore, orderStore, ariOptions, NullLogger<DefaultRenewalInfoService>.Instance);
        var result = await sut.GetRenewalInformationAsync(certificateContainer.AccountId, certificateContainer.CertificateId, TestContext.Current.CancellationToken);

        Assert.NotNull(result);

        Assert.Equal(renewalStart.ToString("O"), result.SuggestedWindow.Start);
        Assert.Equal(renewalStart.Add(TimeSpan.FromDays(2)).ToString("O"), result.SuggestedWindow.End);
    }
}
