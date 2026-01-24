using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Tests.Utils.Fakes;

internal class FakeCAAEvaluator : ICAAEvaluator
{
    public Task<CAAEvaluationResult> EvaluateCAA(CAAEvaluationContext caaContext, CancellationToken cancellationToken)
        => Task.FromResult(CAAEvaluationResult.IssuanceAllowed);
}
