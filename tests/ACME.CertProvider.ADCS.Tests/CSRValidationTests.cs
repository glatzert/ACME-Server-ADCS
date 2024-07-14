using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TGIT.ACME.Protocol.Model;
using Th11s.ACMEServer.CertProvider.ADCS;

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

        [Fact]
        public async Task CSR_has_no_CN_but_matching_SAN()
        {
            var order = new Order("test-account",
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de")
                ]);

            var csr = """
                MIIC9jCCAd4CAQAwQjELMAkGA1UEBhMCREUxDzANBgNVBAgMBkhlc3NlbjESMBAG
                A1UEBwwJV2llc2JhZGVuMQ4wDAYDVQQKDAVUaDExczCCASIwDQYJKoZIhvcNAQEB
                BQADggEPADCCAQoCggEBAMzTOHAPKjGqWrnMT4uSbKQLjShqXaLIWSWEUguBoxWh
                OZHMHRRrKomF9N2Vo70SSM7qk6edjtWDYW7BrqWX0j9NgUC9cWrdV1bTSu6fgORP
                lP4hvcYbEi4UyZv169/7JdJVNPs2cHm1mFmP7SZUo4VuVNw2pRYlfbd/oYA6bJ3E
                7U4l9K8zTOs4s3PckZIByaRRoFv0QCPOsz5bsqAxrLyXep7sPY1jNm8pxgVZQO0z
                e4PxX8vhbF/b1sAvVOpBnEMjUshWLJ3nJJoEEOqju1/EUw7lS969y7pRKbNSFnus
                2bJ5uP3h9zE23zuRXcH3kME+DbRW3RYHdLeKob0MyMMCAwEAAaBvMG0GCSqGSIb3
                DQEJDjFgMF4wDgYDVR0PAQH/BAQDAgWgMCAGA1UdJQEB/wQWMBQGCCsGAQUFBwMB
                BggrBgEFBQcDAjAqBgNVHREEIzAhghBleGFtcGxlLnRoMTFzLmRlgg10ZXN0LnRo
                MTFzLmRlMA0GCSqGSIb3DQEBCwUAA4IBAQCRe2StnwPE4CdhUav8PX6GAfcVAoH0
                Kzgm3Sz/gUugp45t54BNYYOCGwjys3wrEMwTPBy8MTWK67X592x/U9RY9FOv3EtG
                fSjWDEI0MwyI0gyoAGnWNj43UckBrRsM4g5NByTgrzAy9f99EWH8jWYOWlvhf9U+
                jyStc5W17WrR/Dwwl8Auzl2eT08zOldYQx4SE7g4hFQW2yOcBWQwrrKBuNQxuJqz
                y4dM4eq5EaaInSWVHCSLy2KKF1G7Pv+eEa8ebxez1UNLc6rxLIb9LooRwcVfFg9E
                h1RlUx6P/NSJCi8oxyyU9fTpTddI/KK6GNM6/R7Gaf8q6sXfxA3VAQWe
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task CSR_has_matching_CN_but_no_SAN()
        {
            var order = new Order("test-account",
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de")
                ]);

            var csr = """
                MIIC/TCCAeUCAQAwdTEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTEWMBQGA1UE
                AwwNdGVzdC50aDExcy5kZTELMAkGA1UEBhMCREUxDzANBgNVBAgMBkhlc3NlbjES
                MBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYDVQQKDAVUaDExczCCASIwDQYJKoZIhvcN
                AQEBBQADggEPADCCAQoCggEBALESRnpwN2FQ/j7ffGfs3Yqp1qbXxSAFPl9UXuxK
                XajBPa0VU9xt3rjcPHbtL/kttAfWnwVJzdTpnGtftQ0Ofx9uX0jClUXyShJtQbNB
                dx/CWd4cip8dMBhv+9AIXAO9OfVTeyTUlnupit5Dr3it2AJ6K+U9Ja+acdQ4NvIP
                tmsTsD5be62OsAsodmzQIV0ej232IJuS7Eq41CqUg6sVbGKjdXGZAQEf/T7ubBHM
                FEE3aWVNkfmnRH52KaJPJ7lsLCUv00AwSSrJOb+LOWboMlFJVR3AFGR7r2+0EPcU
                aFsyGwg4lzgFZhtf3YICPEZjr1wvHBlUKd9Z+sgr9RsyDcECAwEAAaBDMEEGCSqG
                SIb3DQEJDjE0MDIwDgYDVR0PAQH/BAQDAgWgMCAGA1UdJQEB/wQWMBQGCCsGAQUF
                BwMBBggrBgEFBQcDAjANBgkqhkiG9w0BAQsFAAOCAQEAMbd3HeSSyon8ogXjqti1
                y8K3GxAd5c6gpP0etaFczhrPYc+I9B6FisxmCOK9oZdeUuY53ieZ3Bk+iKT+dxXH
                KMJcQgfcHg2Rv7nDz0boG1z+S5ZsLBbrzwS4sl0geYVBlzLZGqy8orkLrnzGIUos
                dPyGSl7a0uNIbPAkuwHTPAj7OlTmzRwBHWVK+sgK7TREl2J7cZc50biX2OuC6aDV
                MJZKr8I9c60DTKevoyV52FLh5WmV8eTUsxZNB8vc4icQ50ZJ2oIDypPveGym4jcK
                IgG6WbdokMRNIN8difQJOLTfuvNUYWZm/9xEIxsZDOLDeEe4EA7ct2gJcS5JtWwf
                Qg==
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task CSR_has_one_matching_CN_and_one_matching_SAN()
        {
            var order = new Order("test-account",
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de")
                ]);

            var csr = """
                MIIC/zCCAecCAQAwXTEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTELMAkGA1UE
                BhMCREUxDzANBgNVBAgMBkhlc3NlbjESMBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYD
                VQQKDAVUaDExczCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAKt5IycG
                5ISsQXhbPhOckWiYq7GVgh7LapysCdlaCAno2pKM4t0ZAfP3VZeRrAGuxHWryjiV
                DT7qPuXaf7klt17ADN/N6GZzQC2wn6yxRn0d/crC0ShMJtEAzfX36Al9AU01D9ZE
                wIKmjrielzgUf8IZzjwJzZaEYO+vcDHvm9hm0yXp6rFcIA55x2DCYE5agzb5Tp8z
                vICErqfKLLJungi+mxWyUb8P8Cb5R6vjZwZo/k5kvx0XI3L1g8ypbi/FE0aaFyt7
                q/oXIj9PoZLr/IbVNGemlY51Ui92T0ZYBS3ROGzERXA2dpu7reMCkt/61yMiPlAj
                P+FLXcv4JXh+7PUCAwEAAaBdMFsGCSqGSIb3DQEJDjFOMEwwDgYDVR0PAQH/BAQD
                AgWgMCAGA1UdJQEB/wQWMBQGCCsGAQUFBwMBBggrBgEFBQcDAjAYBgNVHREEETAP
                gg10ZXN0LnRoMTFzLmRlMA0GCSqGSIb3DQEBCwUAA4IBAQAhWF8lUV7J1iOkojaD
                5wmzCDHzkGMURFFSPK7oH+L+sLKEE7FO0dZ6MGvEhoDt3dQEMiRlQ+ZtJEwQIMdk
                c/2JVKWugGktmJvtbi6M3ZLzVdB2PX2p1WDJO/0fn72V9OQ64ro5FUO0r5uFa9q9
                pI04AjkkBYJ9EaaJ0pH1oMG7ppQaSbh2mljDpFy5XrxIh7lGABzF2upRw9FDa0LM
                nRbcLlcy+xl0sEoOLrUhhYrp+lWbbWYe2PyHkF2Kumf2fqyuZEwQlf5uyPKesYQT
                ONHJa8jeuGGQEDv+XidE3Xz3bJD4FTNs1ZOG0UaV5Lslu5a578lHVDtuhGEsuPPK
                VdcO
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task CSR_too_much_SANs()
        {
            var order = new Order("test-account",
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de")
                ]);

            var csr = """
                MIIDETCCAfkCAQAwXTEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTELMAkGA1UE
                BhMCREUxDzANBgNVBAgMBkhlc3NlbjESMBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYD
                VQQKDAVUaDExczCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBALWp6cJq
                StV34kwMDfM++CuGvb9qmdzq/3qPn+q/Czv09vFa2asSaemmDK1Jk1JC0dM9lsxz
                iUlCvgYSTAYCawwZBGGIxjrAZurl12gBOla5oQ27l/2BoNTdwX+GFfHnwHcZ7QKH
                qnYp5gbZuUM034T1evZNGIkXhpSsPDwieJBxdWnB5cC7OuTlUYQZvruo7O9tzaUq
                Tf8GNeznpu2MT1AeygKtzjG5gsYlpyFCugb+0RjJg8mDXig0KH5J10zb6ysKxYy1
                GIxrQ78I9s/T7NWXqebXoBRkPRIKJtAV1N9Ir7m+lb/DWoeuXS9PcUrJsWmI7UmY
                f2T0DjQ3q73/R2sCAwEAAaBvMG0GCSqGSIb3DQEJDjFgMF4wDgYDVR0PAQH/BAQD
                AgWgMCAGA1UdJQEB/wQWMBQGCCsGAQUFBwMBBggrBgEFBQcDAjAqBgNVHREEIzAh
                gg10ZXN0LnRoMTFzLmRlghBpbnZhbGlkLnRoMTFzLmRlMA0GCSqGSIb3DQEBCwUA
                A4IBAQCu65uC1FJekKVhlLwjwYzInFrKhYSmelCCOWCUDBmllxZNB7q9ZvX7E7U3
                BjNBia4dp4LRff0Z0UAwfWZbM+ANQaEl5b8N66VW6ET/0lf0TLOgCko/q/db/tot
                GbEm8N94SqOrPRE47W7T6z14z+tv4Sk4uSUAneCzGZoI9RjbBUhFTQVend36KsWX
                3E1HiB3jHPl8elH9X9f1C1rCIyw/WJh7z/b3UVjr56fwNp0wAFtDBdXMqZyZ8W83
                Qy8GhkZcWQ2iZ9xmiDIjxAv0Z4bSP11V2HCF2hscoN0qGYTOx0RXd/UlgM3eoM8h
                IHDU6xvWhX+2ZYJ39zytLO0e4P+j
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task CSR_too_much_CNs()
        {
            var order = new Order("test-account",
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de")
                ]);

            var csr = """
                MIIDGjCCAgICAQAweDEZMBcGA1UEAwwQZXhhbXBsZS50aDExcy5kZTEZMBcGA1UE
                AwwQaW52YWxpZC50aDExcy5kZTELMAkGA1UEBhMCREUxDzANBgNVBAgMBkhlc3Nl
                bjESMBAGA1UEBwwJV2llc2JhZGVuMQ4wDAYDVQQKDAVUaDExczCCASIwDQYJKoZI
                hvcNAQEBBQADggEPADCCAQoCggEBALi/og2QcCCw3kNAdoUcY69W6fPPPhjaMiAj
                X8xXmUtv4/aaMWYHP7wkL3hW5LRWazn05DDT1z8odEajuygOPrSWhXrsTiJ5oRS4
                9lLL17MDpYq95SwOS+BC7F9d+i4pJwMdn5FqnMxdu/jpxmpGyAL6cmmyIEfpHCBr
                BQ2qGaovGjg2QFwubbhuTOBlXvBurA49HY8bUd1NRfHiqZapg2bcElkQKssVhoRu
                QzGbg230z4PzfK1JIIJlDRo8CzK4jZBaQdD0UhTEJzz7UdxfobIaPu47kpi22Aqg
                K/BHkH5D9tMD1CKsWsTRynkcpuxgu6xqg3FR+43rmdaVtQ3I+Y0CAwEAAaBdMFsG
                CSqGSIb3DQEJDjFOMEwwDgYDVR0PAQH/BAQDAgWgMCAGA1UdJQEB/wQWMBQGCCsG
                AQUFBwMBBggrBgEFBQcDAjAYBgNVHREEETAPgg10ZXN0LnRoMTFzLmRlMA0GCSqG
                SIb3DQEBCwUAA4IBAQBwPEA3UvpBFsB4RQlh3sbQouF7jxm1K1E0+9NGDSQs3/V5
                lFdowhdtNs+CAcDy/xnGm54jUpXomtu+wNDhrDLp+G9XcFWUYkolSqpzCmN4vVgt
                1Wr7zLU3U3ILfG38UbND+GT3TUNeKvWepcrfKvAeiFY0NSYVA5e7xgESKGSpxsdr
                8/NgFgTdqjXAXiNpzzNO3pTyVhs1pXa4k37cVzpifGkRYdUbSx8KXmNQhPEnaEOE
                RmJXtJG/mncIzbleZUtZBXHKZfZGPHPxwftLsVhWWLtJ8wK4ICdIJAdQmBGgSSJA
                gINqM4ulmkIR+sLtTLSObXn0nmhLwYsdpdvdvOIh
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task CSR_without_subject_with_matching_san()
        {
            var order = new Order("test-account",
                [
                    new Identifier("dns", "example.th11s.de"),
                    new Identifier("dns", "test.th11s.de")
                ]);

            var csr = """
                MIICtDCCAZwCAQAwADCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBANOh
                gf773ZHFbWVDv4GfqTf4qbjJOW7ldpiU39S6uysADM0sqaqvW1vVm5LYeZSqfiWl
                k5W00IcsMbRRAqZQ9pavSBTOMnYkNpxFmCWrQg1BduDejGcJeWqSzO0bxfEiW9N4
                3R1Hsp0Zk0ItBWJ7nbmDoZ75Mh7Sp4cv2I4I8gFDlu6JHSezvFXuz8OqsLQs5735
                Wx391XQJLwRduKwpm/Cru9mo1xZv54uVFfpYUR9EUgNlT61O5lTzvk9vYNjqA4CN
                EjQi9DUsCSmh43sBJgLLuCq8XBXqUtW4CUATB1+Wmx3cN7uRUXkcOvVRTF/I12vv
                FpSEAFO55tdCObNAXk0CAwEAAaBvMG0GCSqGSIb3DQEJDjFgMF4wDgYDVR0PAQH/
                BAQDAgWgMCAGA1UdJQEB/wQWMBQGCCsGAQUFBwMBBggrBgEFBQcDAjAqBgNVHREE
                IzAhghBleGFtcGxlLnRoMTFzLmRlgg10ZXN0LnRoMTFzLmRlMA0GCSqGSIb3DQEB
                CwUAA4IBAQDL0gFhAyXKAwMuFewBlfHeuf+eNneGex9B8r4eddjS8FwLoL0ROjzv
                CwLIsdalTghg5MnkraS1MHuTgCOd5RGIjT87WiZU6hyWXDud5TxFTaqbMsjWcY67
                9OUPwmslls3ZqhzFyuh69U17FR1Z2OblCz+Q/1/hCAuwMKGlZ2/efKHGwUXc5H5r
                z0VNAg6EQekbll8443ahEAuby+x0iGI4CS9EvjwApiI23v2VLiCToiccpaRlIf6q
                Zw7xCuXQl4SrLCXgzTVF/v65W38Zv0geXQX219imt1SN/l0y4aRtitDg1s3ZpA6h
                5f96H61n8lkFByUzV6fdZWWJRa0Yx7+g
                """;


            var sut = new CSRValidator(Options.Create(new ADCSOptions()), NullLogger<CSRValidator>.Instance);
            var result = await sut.ValidateCsrAsync(order, csr, default);

            Assert.True(result.IsValid);
        }
    }
}