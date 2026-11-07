using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public interface IRenewalInfoService
{
    Task<RenewalInfo> GetRenewalInformationAsync(AccountId accountId, CertificateId certificateId, CancellationToken requestAborted);
}

