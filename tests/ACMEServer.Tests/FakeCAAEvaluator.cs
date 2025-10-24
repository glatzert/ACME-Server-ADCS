using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;

namespace Th11s.AcmeServer.Tests
{
    internal class FakeCAAEvaluator : ICAAEvaluator
    {
        public Task<bool> IsCAAAllowingCertificateIssuance(Identifier identifier, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}
