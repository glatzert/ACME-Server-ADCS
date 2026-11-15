using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services;

public class DefaultRenewalInfoService : IRenewalInfoService
{
    private readonly ICertificateStore _certificateStore;
    private readonly IOrderStore _orderStore;
    private readonly IOptions<ACMEServerOptions> _options;
    private readonly ILogger<DefaultRenewalInfoService> _logger;


    public DefaultRenewalInfoService(
        ICertificateStore certificateStore,
        IOrderStore orderStore,
        IOptions<ACMEServerOptions> options,
        ILogger<DefaultRenewalInfoService> logger)
    {
        _certificateStore = certificateStore;
        _orderStore = orderStore;
        _options = options;
        _logger = logger;
    }


    public async Task<RenewalInfo> GetRenewalInformationAsync(AccountId accountId, CertificateId certificateId, CancellationToken ct)
    {
        var certificateContainer = await _certificateStore.LoadCertificateAsync(certificateId, ct)
            ?? throw new NotFoundException();

        if (certificateContainer.AccountId != accountId)
        {
            throw new NotAllowedException();
        }

        var x509certificateCollection = X509CertificateLoader.LoadPkcs12Collection(certificateContainer.X509Certificates, null);
        var leafCertificate = x509certificateCollection.GetLeafCertificate();

        // The config allows to define planned renewals. If we have one, well return that.
        var plannedRenewalInfo = await GetPlannedRenewalInformationAsync(certificateContainer, leafCertificate, ct);
        if (plannedRenewalInfo is not null)
        {
            return plannedRenewalInfo;
        }

        var (start, end) = CalculateRenewalWindow(leafCertificate.NotBefore, leafCertificate.NotAfter);
        return new RenewalInfo()
        {
            SuggestedWindow = new RenewalPeriod()
            {
                Start = start.ToString("O", CultureInfo.InvariantCulture),
                End = end.ToString("O", CultureInfo.InvariantCulture)
            }
        };
    }

    private async Task<RenewalInfo?> GetPlannedRenewalInformationAsync(CertificateContainer certificateContainer, X509Certificate2 leafCertificate, CancellationToken ct)
    {
        var plannedRenewals = _options.Value.ARI.PlannedRenewals;
        foreach(var plannedRenewal in plannedRenewals)
        {
            // If the certificate is newer than the planned renewal, we skip it.
            if (leafCertificate.NotBefore > plannedRenewal.NotBeforeOlderThan)
            {
                continue;
            }

            // If a profile name is defined, we check if the certificate has that profile name.
            if (!string.IsNullOrWhiteSpace(plannedRenewal.ProfileName))
            {
                var order = await _orderStore.LoadOrderAsync(certificateContainer.OrderId, ct)
                    ?? throw new NotFoundException();

                if (order.Profile != new ProfileName(plannedRenewal.ProfileName))
                {
                    continue;
                }
            }

            var plannedRenewalEnd = plannedRenewal.RenewalStart.Add(plannedRenewal.RenewalPeriod ?? _options.Value.ARI.DefaultRenewalPeriod);

            // We always return the first matching planned renewal.
            return new RenewalInfo()
            {
                SuggestedWindow = new RenewalPeriod()
                {
                    Start = plannedRenewal.RenewalStart.ToString("O", CultureInfo.InvariantCulture),
                    End = plannedRenewalEnd.ToString("O", CultureInfo.InvariantCulture)
                },

                ExplanationUrl = plannedRenewal.ExplanationUrl
            };
        }

        return null;
    }

    private (DateTimeOffset Start, DateTimeOffset End) CalculateRenewalWindow(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var ariConfig = _options.Value.ARI;

        var start = CalculateRenewalStart(notBefore, notAfter);

        var end = start.Add(ariConfig.DefaultRenewalPeriod);
        if (end > notAfter)
        {
            _logger.RenewalEndWasGreaterThanNotAfter();
            end = notAfter;
        }

        return (start, end);
    }


    private DateTimeOffset CalculateRenewalStart(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var ariConfig = _options.Value.ARI;
        DateTimeOffset start;

        if (ariConfig.TryGetDefaultRenewalTimespan(out var renewalTimespan))
        {
            start = notAfter.Add(-renewalTimespan);
        }
        else {
            if (!ariConfig.TryGetDefaultRenewalPercentage(out var percentage))
            {
                percentage = ARIConfiguration.DefaultRenewalStartPercentage;
                _logger.NoValidRenewalStartConfigurationFound();
            }

            var validityPeriod = notAfter - notBefore;
            var daysBeforeExpiration = Math.Max(validityPeriod.TotalDays * (percentage / 100.0), 1.0);

            start = notAfter.AddDays(-daysBeforeExpiration);
        }

        if (start < notBefore)
        {
            start = notBefore;
            _logger.RenewalStartWasLessThanNotBefore();
        }

        return start;
    }
}
