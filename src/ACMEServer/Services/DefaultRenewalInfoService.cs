using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Extensions;
using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services;

public class DefaultRenewalInfoService : IRenewalInfoService
{
    private readonly ICertificateStore _certificateStore;

    public DefaultRenewalInfoService(ICertificateStore certificateStore)
    {
        _certificateStore = certificateStore;
    }

    public async Task<RenewalInfo> GetRenewalInformationAsync(AccountId accountId, CertificateId certificateId, CancellationToken ct)
    {
        var certificateContainer = await _certificateStore.LoadCertificateAsync(certificateId, ct)
            ?? throw new NotFoundException();

        if (certificateContainer.AccountId != accountId)
        {
            throw new NotAllowedException();
        }

        // TODO: The admins should have a way to force a renewal for all certificates older than a certain date, or for all certificates with a certain profile.

        var x509certificateCollection = X509CertificateLoader.LoadPkcs12Collection(certificateContainer.X509Certificates, null);
        var leafCertificate = x509certificateCollection.GetLeafCertificate();

        // TODO: The admins should have a way to define a renewal window. (Perhaps for each profile?)
        
        var validityPeriod = leafCertificate.NotAfter - leafCertificate.NotBefore;
        var daysBeforeExpiration = Math.Max(validityPeriod.TotalDays / 4.0, 1.0);
        var renewalDay = leafCertificate.NotAfter.AddDays(-daysBeforeExpiration);

        return new RenewalInfo()
        {
            SuggestedWindow = new RenewalPeriod()
            {
                Start = renewalDay.ToString("O", CultureInfo.InvariantCulture),
                End = renewalDay.AddDays(1).ToString("O", CultureInfo.InvariantCulture)
            }
        };
    }
}
