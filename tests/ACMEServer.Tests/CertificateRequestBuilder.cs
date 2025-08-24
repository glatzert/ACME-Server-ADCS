using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Th11s.AcmeServer.Tests
{
    internal class CertificateRequestBuilder
    {
        public CertificateRequestBuilder() { }

        private List<string> _commonNames = [];
        private List<string> _subjectParts = [];

        private SubjectAlternativeNameBuilder _subjectAlternativeNameBuilder = new();
        private bool HasSubjectAlternativeNames { get; set; }

        private ECDsa? _privateKey;


        public CertificateRequestBuilder WithPrivateKey(ECDsa privateKey)
        {
            _privateKey = privateKey;
            return this;
        }

        public CertificateRequestBuilder WithDefaultSubjectSuffix()
            => this.WithSubjectPart("O=Th11s")
            .WithSubjectPart("L=Niedernhausen")
            .WithSubjectPart("ST=Hessen")
            .WithSubjectPart("C=DE");

        public CertificateRequestBuilder WithSubjectPart(string kvp)
        {
            _subjectParts.Add(kvp);
            return this;
        }

        public CertificateRequestBuilder WithCommonName(string commonName)
        {
            _commonNames.Add($"CN={commonName}");
            return this;
        }

        public CertificateRequestBuilder WithDnsName(string san)
        {
            _subjectAlternativeNameBuilder.AddDnsName(san);
            HasSubjectAlternativeNames = true;
            return this;
        }

        public CertificateRequestBuilder WithIpAddress(System.Net.IPAddress san)
        {
            _subjectAlternativeNameBuilder.AddIpAddress(san);
            HasSubjectAlternativeNames = true;
            return this;
        }

        public CertificateRequestBuilder WithEmailAddress(string san)
        {
            _subjectAlternativeNameBuilder.AddEmailAddress(san);
            HasSubjectAlternativeNames = true;
            return this;
        }

        public CertificateRequestBuilder WithUri(Uri san)
        {
            _subjectAlternativeNameBuilder.AddUri(san);
            HasSubjectAlternativeNames = true;
            return this;
        }

        public CertificateRequestBuilder WithUserPrincipalName(string san)
        {
            _subjectAlternativeNameBuilder.AddUserPrincipalName(san);
            HasSubjectAlternativeNames = true;
            return this;
        }

        public string AsBase64Url()
        {
            var subject = string.Join(",", _commonNames.Concat(_subjectParts));

            var certificateRequest = new CertificateRequest(
                subject,
                _privateKey ?? ECDsa.Create(),
                HashAlgorithmName.SHA256);

            if (HasSubjectAlternativeNames)
            {
                certificateRequest.CertificateExtensions.Add(
                    _subjectAlternativeNameBuilder.Build());
            }

            var csrBytes = certificateRequest.CreateSigningRequest();
            return Base64UrlTextEncoder.Encode(csrBytes);
        }
    }
}
