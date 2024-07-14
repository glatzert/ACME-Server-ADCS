namespace TGIT.ACME.Protocol.Model
{
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


        public bool IsValid { get; }

        public AcmeError? Error { get; }


        public static AcmeValidationResult Success() 
            => new AcmeValidationResult();

        public static AcmeValidationResult Failed(AcmeError error)
            => new AcmeValidationResult(error);
    }
}
