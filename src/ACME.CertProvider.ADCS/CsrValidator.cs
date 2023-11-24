using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using CertEnroll = CERTENROLLLib;

namespace TGIT.ACME.Protocol.IssuanceServices.ADCS
{
    public class CsrValidator : ICsrValidator
    {
        private readonly IOptions<ADCSOptions> _options;
        private readonly ILogger<CsrValidator> _logger;

        public CsrValidator(IOptions<ADCSOptions> options, ILogger<CsrValidator> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task<(bool isValid, AcmeError? error)> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Attempting validation of CSR {csr}");
            try
            {
                var request = new CertEnroll.CX509CertificateRequestPkcs10();

                request.InitializeDecode(csr, CertEnroll.EncodingType.XCN_CRYPT_STRING_BASE64);
                request.CheckSignature();

                if (!SubjectIsValid(request, order))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid CN.");
                    return Task.FromResult((false, new AcmeError("badCSR", "CN Invalid.")));
                }

                if (!SubjectAlternateNamesAreValid(request, order))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                    return Task.FromResult((false, new AcmeError("badCSR", "SAN Invalid.")));
                }

                _logger.LogDebug("CSR Validation succeeded.");
                return Task.FromResult((true, (AcmeError?)null));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Validation of CSR failed with exception.");
                return Task.FromResult((false, new AcmeError("badCSR", "CSR could not be read.")));
            }
        }

        private bool SubjectIsValid(CertEnroll.CX509CertificateRequestPkcs10 request, Order order)
        {
            try
            {
                var validCNs = order.Identifiers.Select(x => x.Value)
                    .Concat(
                        order.Identifiers.Where(x => x.IsWildcard)
                        .Select(x => x.Value[2..])
                    )
                    .Select(x => "CN=" + x)
                    .ToList();

                return validCNs.Any(x => request.Subject.Name.Equals(x) ||
                    (_options.Value.AllowCNSuffix && request.Subject.Name.StartsWith(x)));
            }
            // This is thrown, if there is no subject.
            catch (Exception)
                when (_options.Value.AllowEmptyCN)
            {
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occured during validation of CSR Subject.");
                return false;
            }
        }

        private bool SubjectAlternateNamesAreValid(CertEnroll.CX509CertificateRequestPkcs10 request, Order order)
        {
            try
            {
                var identifiers = order.Identifiers.Select(x => x.Value).ToList();

                foreach (var x509Ext in request.X509Extensions.OfType<CertEnroll.CX509ExtensionAlternativeNames>())
                {
                    foreach(var san in x509Ext.AlternativeNames.Cast<CertEnroll.CAlternativeName>())
                    {
                        //TODO: If we support more than one identifier type, we'll need to branch here
                        if (san.Type != CertEnroll.AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME)
                            return false;

                        if (!identifiers.Contains(san.strValue))
                            return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured during validation of CSR SANs.");
                return false;
            }
        }
    }
}
