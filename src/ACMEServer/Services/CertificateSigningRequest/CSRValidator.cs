using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.CertificateSigningRequest
{
    public class CSRValidator : ICSRValidator
    {
        private readonly ILogger<CSRValidator> _logger;

        public CSRValidator(ILogger<CSRValidator> logger)
        {
            _logger = logger;
        }

        public Task<AcmeValidationResult> ValidateCsrAsync(Order order, CancellationToken cancellationToken)
        {
            try { 
                var publicKeyValidator = new ExpectedPublicKeyValidator();
                if (!publicKeyValidator.IsValid(validationContext))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid public key.");
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "Public Key Invalid.")));
                }

                var subjectValidator = new SubjectValidator();
                if (!subjectValidator.IsValid(validationContext))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid CN.");
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CN Invalid.")));
                }

                var sanValidator = new AlternateNameValidator();
                if (!sanValidator.AreAllAlternateNamesValid(validationContext))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "SAN Invalid.")));
                }

                // ACME states that all identifiers must be present in either CN or SAN.
                if (!validationContext.AreAllIdentifiersUsed())
                {
                    _logger.LogDebug("CSR validation failed. Not all identifiers where present in either CN or SAN");
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "Missing identifiers in CN or SAN.")));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Validation of CSR failed with exception.");
                return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CSR could not be read.")));
            }

            _logger.LogDebug("CSR Validation succeeded.");
            return Task.FromResult(AcmeValidationResult.Success());
        }
    }


    internal class CSRValidationContext
    {
        public CertificateRequest CertificateRequest { get; }

        public string? SubjectName { get; init; }
        public IReadOnlyList<string>? CommonNames { get; init; }

        public IReadOnlyList<AlternativeName>? AlternativeNames { get; init; }

        public ICollection<Identifier> Identifiers => IdentifierUsageState.Keys;
        private IDictionary<Identifier, bool> IdentifierUsageState { get; }

        public string[] ExpectedPublicKeys { get; private set; } = [];

        private CSRValidationContext(CertificateRequest request, IEnumerable<Identifier> identifiers)
        {
            CertificateRequest = request;
            IdentifierUsageState = identifiers.ToDictionary(x => x, x => false);
        }

        /// <summary>
        /// Flags the given identifier as used in the CSR.
        /// </summary>
        /// <param name="identifier"></param>
        public void SetIdentifierIsUsed(Identifier identifier)
            => IdentifierUsageState[identifier] = true;

        /// <summary>
        /// Checks if all identifiers have been used in the CSR.
        /// </summary>
        public bool AreAllIdentifiersUsed()
            => IdentifierUsageState.All(x => x.Value);


        public static CSRValidationContext Create(Order order)
        {
            if(string.IsNullOrWhiteSpace(order?.CertificateSigningRequest))
            {
                throw AcmeErrors.BadCSR("CSR is empty or null.").AsException();
            }

            var certificateRequest = CertificateRequest.LoadSigningRequest(
                Convert.FromBase64String(order.CertificateSigningRequest), 
                HashAlgorithmName.SHA256, // we'll not sign the request, so this is more a placeholder than anything else
                CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions // this enables loading of extensions, which is required for SAN validation
                );

            return new CSRValidationContext(certificateRequest, order.Identifiers)
            {
                SubjectName = certificateRequest.SubjectName.Name,

                CommonNames = certificateRequest.SubjectName.GetCommonNames(),
                AlternativeNames = certificateRequest.CertificateExtensions.GetSubjectAlternativeNames(),
                    
                ExpectedPublicKeys = [.. order.Authorizations
                    .Select(x => x.Identifier.GetExpectedPublicKey()!)
                    .Where(x => x is not null)
                ]
            };
            
        }
    }

    public record AlternativeName(string OID, byte[] Value);
}
