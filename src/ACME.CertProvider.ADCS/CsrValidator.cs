using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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

                var validIdentifiers = new HashSet<string>();

                if (!SubjectIsValid(request, order, validIdentifiers))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid CN.");
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "CN Invalid.")));
                }

                if (!SubjectAlternateNamesAreValid(request, order, validIdentifiers))
                {
                    _logger.LogDebug("CSR Validation failed due to invalid SAN.");
                    return Task.FromResult(AcmeValidationResult.Failed(new AcmeError("badCSR", "SAN Invalid.")));
                }

                if(validIdentifiers.Count != order.Identifiers.Count)
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



        private bool SubjectIsValid(
            CertEnroll.CX509CertificateRequestPkcs10 request, 
            Order order, 
            ISet<string> validatedIdentifiers)
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

            foreach(var subjectCN in commonNames)
            {
                var matches = validCNs.Where(validCN => 
                    subjectCN.Equals(validCN, StringComparison.OrdinalIgnoreCase) ||
                    (_options.Value.AllowCNSuffix && subjectCN.StartsWith(validCN, StringComparison.OrdinalIgnoreCase))
                );


                if (matches.Any())
                {
                    foreach(var matchedCN in matches)
                    validatedIdentifiers.Add(matchedCN);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }


        private bool SubjectAlternateNamesAreValid(
            CertEnroll.CX509CertificateRequestPkcs10 request, 
            Order order,
            ISet<string> validatedIdentifiers)
        {
            try
            {
                var subjectAlternateNames = new List<CertEnroll.CAlternativeName>();

                foreach (var x509Ext in request.X509Extensions.OfType<CertEnroll.CX509ExtensionAlternativeNames>())
                {
                    foreach (var san in x509Ext.AlternativeNames.Cast<CertEnroll.CAlternativeName>())
                    {
                        subjectAlternateNames.Add(san);
                    }
                }

                var x509ExtensionAlternativeNames = request.X509Extensions
                    .OfType<CertEnroll.CX509ExtensionAlternativeNames>()
                    .ToList();

                //var subjectAlternateNames = x509ExtensionAlternativeNames
                //    .Select(x => x.AlternativeNames)
                //    .Cast<CertEnroll.CAlternativeName>()
                //    .ToList();

                // Currently only SANs with DNSName are allowed.
                if (subjectAlternateNames.Any(x => x.Type != CertEnroll.AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME))
                    return false;

                var validSANs = new List<string>();

                var identifiers = order.Identifiers.Select(x => x.Value).ToList();

                foreach(var identifier in order.Identifiers.Select(x => x.Value))
                {
                    if (!subjectAlternateNames.Any(x => identifier.Equals(x.strValue, StringComparison.OrdinalIgnoreCase)))
                        return false;

                    validSANs.Add(identifier);
                }

                // there may not be additional SANs
                if (subjectAlternateNames.Count > validSANs.Count)
                    return false;

                foreach(var validSAN in validSANs)
                    validatedIdentifiers.Add(validSAN);

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
