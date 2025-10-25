using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model;

public record CAAEvaluationContext(
    AccountId AccountId,
    Identifier Identifier
);
