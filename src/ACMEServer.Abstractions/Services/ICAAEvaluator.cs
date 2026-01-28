using Th11s.ACMEServer.Model.CAA;

namespace Th11s.ACMEServer.Services;

public interface ICAAEvaluator
{
    Task<CAAEvaluationResult> EvaluateCAA(CAAEvaluationContext caaContext, CancellationToken cancellationToken);
}
 