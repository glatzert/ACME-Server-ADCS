using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Services;
using CertEnroll = CERTENROLLLib;

namespace TGIT.ACME.CertProvider.ACDS
{
    public class CsrValidator : ICsrValidator
    {
        private readonly IOptions<AcmeProtocolOptions> _options;
        private readonly ILogger<CsrValidator> _logger;

        public CsrValidator(IOptions<AcmeProtocolOptions> options, ILogger<CsrValidator> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task<(bool isValid, AcmeError? error)> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken)
        {
            try
            {
                var request = new CertEnroll.CX509CertificateRequestPkcs10();

                request.InitializeDecode(csr, CertEnroll.EncodingType.XCN_CRYPT_STRING_BASE64_ANY);
                request.CheckSignature();

                if (!SubjectIsValid(request, order))
                    return Task.FromResult((false, (AcmeError?)new AcmeError("TYPE", "CN Invalid")));

                if (!SubjectAlternateNamesAreValid(request, order))
                    return Task.FromResult((false, (AcmeError?)new AcmeError("TYPE", "SAN Invalid")));

                return Task.FromResult((true, (AcmeError?)null));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
            }

            return Task.FromResult((false, (AcmeError?)new AcmeError("TYPE", "Generic Error")));
        }

        private bool SubjectIsValid(CertEnroll.CX509CertificateRequestPkcs10 request, Order order)
        {
            var validCNs = order.Identifiers.Select(x => x.Value)
                .Concat(order.Identifiers.Where(x => x.IsWildcard).Select(x => x.Value.Substring(2)))
                .Select(x => "CN=" + x)
                .ToList();

            return validCNs.Any(x => request.Subject.Name.Equals(x) || 
                (_options.Value.AllowCNSuffix && request.Subject.Name.StartsWith(x)));
        }

        private bool SubjectAlternateNamesAreValid(CertEnroll.CX509CertificateRequestPkcs10 request, Order order)
        {
            try
            {
                var identifiers = order.Identifiers.Select(x => x.Value).ToList();

                foreach (var x509Ext in request.X509Extensions.Cast<CertEnroll.CX509Extension>())
                {
                    if (x509Ext.ObjectId.Name != CertEnroll.CERTENROLL_OBJECTID.XCN_OID_SUBJECT_ALT_NAME2)
                        return false;

                    var extBase64 = x509Ext.RawData[CertEnroll.EncodingType.XCN_CRYPT_STRING_BASE64];
                    if (string.IsNullOrWhiteSpace(extBase64))
                        return false;

                    var sanNames = new CertEnroll.CX509ExtensionAlternativeNames();
                    sanNames.InitializeDecode(CertEnroll.EncodingType.XCN_CRYPT_STRING_BASE64, extBase64);

                    foreach(var san in sanNames.AlternativeNames.Cast<CertEnroll.CAlternativeName>())
                    {
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
                _logger.LogError(ex, "Error occured during validation of CSR.");
                return false;
            }
        }
    }
}
