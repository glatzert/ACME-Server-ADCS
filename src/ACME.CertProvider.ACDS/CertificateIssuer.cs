using System;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Services;
using CertEnroll = CERTENROLLLib;
using CertCli = CERTCLILib;
using TGIT.ACME.Protocol.Model;
using Microsoft.Extensions.Options;

namespace TGIT.ACME.CertProvider.ACDS
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

        public Task<(byte[]? certificate, AcmeError? error)> IssueCertificate(string csr, CancellationToken cancellationToken)
        {
            try
            {
                var certRequest = new CertCli.CCertRequest();
                var submitResponseCode = certRequest.Submit(CR_IN_BASE64, csr, "", _options.Value.CAServer);

                if(submitResponseCode == 3)
                {
                    var base64Certificate = certRequest.GetCertificate(CR_OUT_BASE64 | CR_OUT_CHAIN);
                    var result = ((byte[]?)Convert.FromBase64String(base64Certificate), (AcmeError?)null);
                    return Task.FromResult(result);
                } 
                else
                {
                    return Task.FromResult(((byte[]?)null, (AcmeError?)null));
                }
            } catch (Exception ex)
            {
                //TODO: handle exceptions
            }

            return Task.FromResult(((byte[]?)null, (AcmeError?)null));
        }
    }
}
