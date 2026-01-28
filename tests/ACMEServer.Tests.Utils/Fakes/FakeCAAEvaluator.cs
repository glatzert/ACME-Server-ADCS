using Th11s.ACMEServer.Model.CAA;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Tests.Utils.Fakes;

internal class FakeCAAEvaluator : ICAAEvaluator
{
    public Task<CAAEvaluationResult> EvaluateCAA(CAAEvaluationContext caaContext, CancellationToken cancellationToken)
        => Task.FromResult(new CAAEvaluationResult(CAARule.IssuanceAllowed));
}
