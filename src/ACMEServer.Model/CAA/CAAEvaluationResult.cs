namespace Th11s.ACMEServer.Model.CAA;

public record CAAEvaluationResult(
    CAARule CAARule,
    string[]? AllowedChallengeTypes)
{
    public CAAEvaluationResult(CAARule caaRule)
        :this(caaRule, null) 
    { }
}

public enum CAARule
{
    NotApplicable,
    IssuanceAllowed,
    IssuanceForbidden,
}