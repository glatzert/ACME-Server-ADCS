﻿using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace ACMEServer.ADCS.IntegrationTests;

internal class FakeCertificateIssuer : ICertificateIssuer
{
    public Task<(byte[]? Certificates, AcmeError? Error)> IssueCertificate(string csr, CancellationToken cancellationToken)
    {
        // Create a self-signed certificate for testing purposes
        using (RSA rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest(
                new X500DistinguishedName($"CN=example.com"),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add extensions to the certificate (optional)
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            // Create the self-signed certificate
            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = notBefore.AddYears(1);
            X509Certificate2 certificate = request.CreateSelfSigned(notBefore, notAfter);

            // Export the certificate with the private key
            return Task.FromResult(((byte[]?)new X509Certificate2(certificate.Export(X509ContentType.Pfx)).Export(X509ContentType.Cert), (AcmeError?) null));
        }
    }
}