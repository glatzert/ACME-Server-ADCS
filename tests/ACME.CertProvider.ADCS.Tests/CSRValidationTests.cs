using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TGIT.ACME.Protocol.IssuanceServices.ADCS;
using TGIT.ACME.Protocol.Model;

namespace ACME.CertProvider.ADCS.Tests
{
    public class CSRValidationTests
    {
        [Fact]
        public async Task CSR_And_Order_Match()
        {
            var order = new Order("test-account", 
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de")
                ]);

            var csr = """
                MIIDMjCCAhoCAQAwfjEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTELMAkGA1UE
                BhMCREUxDzANBgNVBAgMBkhlc3NlbjESMBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYD
                VQQKDAVUaDExczEfMB0GCSqGSIb3DQEJARYQdGgxMXNAb3V0bG9vay5kZTCCASIw
                DQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBANLXAfqXbaYOcsX3H0VE+52UCyq6
                BQ4aRvaQGol5dNNsiUIlELpYenCKQnjHk/FZo0BSsgBN0TGTx0POl4Y2vpKBqvhZ
                Wux7e4kCp54DpCK/dTnEGTJ18Cds2MUrzoVWzQFAp5ZDche6I4jZrO/zDlg/2WDI
                6qgp2+q+P5eg3RMo50N2pi/eSb3L/bDJXO9Vk75Tw9kueHr5D1I464byPp9dLOnS
                ZeRUqXC6X2rfIO6wOkfEuEBzJj8SOvER3PsG68gsi51vmvPvheW4G8oMUwytN3JE
                +fuZ2JxdBgoSvVBArxH35rgWiak3ooaCXeDMY6ZL0dCQjzNO7O+ogZ+KQpkCAwEA
                AaBvMG0GCSqGSIb3DQEJDjFgMF4wDgYDVR0PAQH/BAQDAgWgMCAGA1UdJQEB/wQW
                MBQGCCsGAQUFBwMBBggrBgEFBQcDAjAqBgNVHREEIzAhghBleGFtcGxlLnRoMTFz
                LmRlgg10ZXN0LnRoMTFzLmRlMA0GCSqGSIb3DQEBCwUAA4IBAQC6jbrvu+exsPxv
                bXwic5/ahosW555aUAahNr2IHNh0PfcMrxGvAjcOGLMD6wETC/7aQQg2WTHtFXFU
                F2Bxv4wacOtCnqhhPKN/QJdsr8JBU8FesSbQ+JR+KKYaPZglf254qHOui/gT8g4v
                A5u0gYfY2K0D/8S41BHwmGWXc1QTyORxsGIWPp0+Sd/W+Mr8OlNgCsZzZ2zyt7xh
                4iQGlrI/RvB9ycOX3aasWtmENNXNPyWlaUag5l1+FuUKGvAu9vbRIBqVRWuKF+8h
                W8QV7psGYoAivFVTgySGhPrwofiDU11hwx9TjDgEfN+yw48CVr1YYzCE/GKhZ+v0
                istQPI5p
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.True(result.IsValid);
        }
        
        [Fact]
        public async Task Order_has_more_identifiers_than_CSR()
        {
            var order = new Order("test-account", 
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de"),
                    new Identifier("dns", "test1.th11s.de")
                ]);

            var csr = """
                MIIDMjCCAhoCAQAwfjEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTELMAkGA1UE
                BhMCREUxDzANBgNVBAgMBkhlc3NlbjESMBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYD
                VQQKDAVUaDExczEfMB0GCSqGSIb3DQEJARYQdGgxMXNAb3V0bG9vay5kZTCCASIw
                DQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBANLXAfqXbaYOcsX3H0VE+52UCyq6
                BQ4aRvaQGol5dNNsiUIlELpYenCKQnjHk/FZo0BSsgBN0TGTx0POl4Y2vpKBqvhZ
                Wux7e4kCp54DpCK/dTnEGTJ18Cds2MUrzoVWzQFAp5ZDche6I4jZrO/zDlg/2WDI
                6qgp2+q+P5eg3RMo50N2pi/eSb3L/bDJXO9Vk75Tw9kueHr5D1I464byPp9dLOnS
                ZeRUqXC6X2rfIO6wOkfEuEBzJj8SOvER3PsG68gsi51vmvPvheW4G8oMUwytN3JE
                +fuZ2JxdBgoSvVBArxH35rgWiak3ooaCXeDMY6ZL0dCQjzNO7O+ogZ+KQpkCAwEA
                AaBvMG0GCSqGSIb3DQEJDjFgMF4wDgYDVR0PAQH/BAQDAgWgMCAGA1UdJQEB/wQW
                MBQGCCsGAQUFBwMBBggrBgEFBQcDAjAqBgNVHREEIzAhghBleGFtcGxlLnRoMTFz
                LmRlgg10ZXN0LnRoMTFzLmRlMA0GCSqGSIb3DQEBCwUAA4IBAQC6jbrvu+exsPxv
                bXwic5/ahosW555aUAahNr2IHNh0PfcMrxGvAjcOGLMD6wETC/7aQQg2WTHtFXFU
                F2Bxv4wacOtCnqhhPKN/QJdsr8JBU8FesSbQ+JR+KKYaPZglf254qHOui/gT8g4v
                A5u0gYfY2K0D/8S41BHwmGWXc1QTyORxsGIWPp0+Sd/W+Mr8OlNgCsZzZ2zyt7xh
                4iQGlrI/RvB9ycOX3aasWtmENNXNPyWlaUag5l1+FuUKGvAu9vbRIBqVRWuKF+8h
                W8QV7psGYoAivFVTgySGhPrwofiDU11hwx9TjDgEfN+yw48CVr1YYzCE/GKhZ+v0
                istQPI5p
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.False(result.IsValid);
        }
        
        [Fact]
        public async Task Order_has_less_identifiers_than_CSR()
        {
            var order = new Order("test-account", 
                [
                    new Identifier("dns", "example.th11s.de")
                ]);

            var csr = """
                MIIDMjCCAhoCAQAwfjEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTELMAkGA1UE
                BhMCREUxDzANBgNVBAgMBkhlc3NlbjESMBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYD
                VQQKDAVUaDExczEfMB0GCSqGSIb3DQEJARYQdGgxMXNAb3V0bG9vay5kZTCCASIw
                DQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBANLXAfqXbaYOcsX3H0VE+52UCyq6
                BQ4aRvaQGol5dNNsiUIlELpYenCKQnjHk/FZo0BSsgBN0TGTx0POl4Y2vpKBqvhZ
                Wux7e4kCp54DpCK/dTnEGTJ18Cds2MUrzoVWzQFAp5ZDche6I4jZrO/zDlg/2WDI
                6qgp2+q+P5eg3RMo50N2pi/eSb3L/bDJXO9Vk75Tw9kueHr5D1I464byPp9dLOnS
                ZeRUqXC6X2rfIO6wOkfEuEBzJj8SOvER3PsG68gsi51vmvPvheW4G8oMUwytN3JE
                +fuZ2JxdBgoSvVBArxH35rgWiak3ooaCXeDMY6ZL0dCQjzNO7O+ogZ+KQpkCAwEA
                AaBvMG0GCSqGSIb3DQEJDjFgMF4wDgYDVR0PAQH/BAQDAgWgMCAGA1UdJQEB/wQW
                MBQGCCsGAQUFBwMBBggrBgEFBQcDAjAqBgNVHREEIzAhghBleGFtcGxlLnRoMTFz
                LmRlgg10ZXN0LnRoMTFzLmRlMA0GCSqGSIb3DQEBCwUAA4IBAQC6jbrvu+exsPxv
                bXwic5/ahosW555aUAahNr2IHNh0PfcMrxGvAjcOGLMD6wETC/7aQQg2WTHtFXFU
                F2Bxv4wacOtCnqhhPKN/QJdsr8JBU8FesSbQ+JR+KKYaPZglf254qHOui/gT8g4v
                A5u0gYfY2K0D/8S41BHwmGWXc1QTyORxsGIWPp0+Sd/W+Mr8OlNgCsZzZ2zyt7xh
                4iQGlrI/RvB9ycOX3aasWtmENNXNPyWlaUag5l1+FuUKGvAu9vbRIBqVRWuKF+8h
                W8QV7psGYoAivFVTgySGhPrwofiDU11hwx9TjDgEfN+yw48CVr1YYzCE/GKhZ+v0
                istQPI5p
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.False(result.IsValid);
        }
    }
}