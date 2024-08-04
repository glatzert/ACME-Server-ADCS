namespace Th11s.ACMEServer.Model.Services
{
    public interface ICSRValidator
    {
        Task<AcmeValidationResult> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken);
    }
}
