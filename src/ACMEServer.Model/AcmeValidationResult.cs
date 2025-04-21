using System.Diagnostics.CodeAnalysis;

namespace Th11s.ACMEServer.Model;

public class AcmeValidationResult
{
    public AcmeValidationResult()
    {
        IsValid = true;
    }

    public AcmeValidationResult(AcmeError error)
    {
        IsValid = false;
        Error = error;
    }


    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsValid { get; }

    public AcmeError? Error { get; }


    public static AcmeValidationResult Success()
        => new();

    public static AcmeValidationResult Failed(AcmeError error)
        => new(error);
}
