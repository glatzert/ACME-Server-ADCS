using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface ICAAEvaluator
{
    Task<bool> IsCAAAllowingCertificateIssuance(Identifier identifier, CancellationToken cancellationToken);
}
