using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public interface ICAAEvaluator
{
    Task<CAAEvaluationResult> EvaluateCAA(CAAEvaluationContext caaContext, CancellationToken cancellationToken);
}

public record CAAEvaluationContext(
    AccountId AccountId,
    Identifier Identifier
);

public enum CAAEvaluationResult
{
    IssuanceAllowed,
    IssuanceForbidden,
}