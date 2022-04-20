using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using CertCli = CERTCLILib;

namespace TGIT.ACME.Protocol.IssuanceServices.ACDS
{
    public sealed class CertificateIssuer : ICertificateIssuer
    {
        private const int CR_IN_BASE64 = 0x1;
        private const int CR_OUT_BASE64 = 0x1;
        private const int CR_OUT_CHAIN = 0x100;

        private readonly IOptions<ACDSOptions> _options;

        public CertificateIssuer(IOptions<ACDSOptions> options)
        {
            _options = options;
        }

        public Task<(byte[]? Certificates, AcmeError? Error)> IssueCertificate(string csr, CancellationToken cancellationToken)
        {
            var result = (Certificates: (byte[]?)null, Error: (AcmeError?)null);

            try
            {
                var certRequest = new CertCli.CCertRequest();
                var attributes = $"CertificateTemplate:{_options.Value.TemplateName}";
                var submitResponseCode = certRequest.Submit(CR_IN_BASE64, csr, attributes, _options.Value.CAServer);

                if(submitResponseCode == 3)
                {
                    var issuerResponse = certRequest.GetCertificate(CR_OUT_BASE64 | CR_OUT_CHAIN);
                    var issuerResponseBytes = Convert.FromBase64String(issuerResponse);

                    var issuerSignedCms = new SignedCms();
                    issuerSignedCms.Decode(issuerResponseBytes);
                    result.Certificates = issuerSignedCms.Certificates.Export(X509ContentType.Pfx);
                } 
                else
                {
                    result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator.");
                }
            } 
            catch (Exception e)
            {
                result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator");
            }

            return Task.FromResult(result);
        }
    }
}
