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


        public Task<AcmeValidationResult> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken)
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
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CN Invalid.")));
                }

                if (!SubjectAlternateNamesAreValid(request, order))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "SAN Invalid.")));
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



        private bool SubjectIsValid(CertEnroll.CX509CertificateRequestPkcs10 request, Order order)
        {
            //We'll only check CNs and ignore other parts of the distinguished name.

            string? subject;
            
            try
            {
                subject = request.Subject.Name;
            }
            // There might be an exception, when the subject is empty.
            catch(Exception) when (_options.Value.AllowEmptyCN)
            {
                subject = null;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occured during validation of CSR Subject.");
                return false;
            }

            // If subject is empty, check if it's acceptable.
            if(subject == null) {
                return _options.Value.AllowEmptyCN;
            }

            // We'll assume, that a cn might be included multiple times, even when that's not common
            // and the issuer will most likely not accept that
            var commonNames = subject.Split(',', StringSplitOptions.TrimEntries)
                .Select(x => x.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(x => string.Equals("cn", x.First(), StringComparison.OrdinalIgnoreCase)) // Check for cn=
                .Select(x => x.Last()) // take =value
                .ToList();

            var validCNs = order.Identifiers.Select(x => x.Value)
                .Concat(
                    order.Identifiers.Where(x => x.IsWildcard)
                    .Select(x => x.Value[2..])
                )
                .ToList();

            if (commonNames.Count == 0)
                return _options.Value.AllowEmptyCN;

            foreach(var cn in commonNames)
            {
                var isCNValid = validCNs.Any(x => 
                    x.Equals(cn, StringComparison.OrdinalIgnoreCase) ||
                    (_options.Value.AllowCNSuffix && cn.StartsWith(x, StringComparison.OrdinalIgnoreCase))
                );

                if (!isCNValid)
                {
                    return false;
                }
            }

            return true;
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
