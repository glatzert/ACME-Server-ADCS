using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.CAA;

public record CAAEvaluationContext(
    AccountId AccountId,
    Identifier Identifier
);
