using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services.CsrValidation;
using Th11s.ACMEServer.Tests.Utils;
using Th11s.ACMEServer.Tests.Utils.Fakes;

namespace Th11s.ACMEServer.Tests.Services.CsrValidation;

/// <summary>
/// Tests for CSR validation.
/// Helpful tools: 
///  - https://certlogik.com/decoder/
///  - https://certificatetools.com/
/// </summary>
public class CSRValidationTests
{
    private static readonly AccountId _testAccountId = new("test-account");

    private readonly FakeOptionSnapshot<ProfileConfiguration> _profileConfiguration = new(
        new()
        {
            ["test-profile"] = new ProfileConfiguration
            {
                SupportedIdentifiers = [IdentifierTypes.DNS, IdentifierTypes.IP],
                ADCSOptions = new()
                {
                    CAServer = "CA\\SERVER",
                    TemplateName = "Template"
                },
            }
        });

    private Order CreateOrder(params Identifier[] identifiers) =>
        new(_testAccountId, identifiers)
        {
            Profile = new("test-profile"),
        };


    [Fact]
    public async Task CSR_and_matching_order_are_valid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de"),
                new Identifier("ip", "198.51.100.42"),
                new Identifier("ip", "[2001:db8::42]")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDefaultSubjectSuffix()
           .WithCommonName("example.th11s.de")
           .WithDnsName("test.th11s.de")
           .WithIpAddress(IPAddress.Parse("198.51.100.42"))
           .WithIpAddress(IPAddress.Parse("[2001:db8::42]"))
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }


    [Fact]
    public async Task CSR_does_use_expected_Key_is_valid()
    {
        var order = CreateOrder(
            new Identifier("dns", "example.th11s.de")
        );

        var privateKey = ECDsa.Create();

        order.Identifiers[0].Metadata[Identifier.MetadataKeys.PublicKey] = Convert.ToBase64String(privateKey.ExportSubjectPublicKeyInfo());

        order.Authorizations.Add(new(order, order.Identifiers[0], DateTimeOffset.Now.AddHours(1)) {
            Status = AuthorizationStatus.Valid,
        });
        

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithPrivateKey(privateKey)
           .WithCommonName("example.th11s.de")
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }


    [Fact]
    public async Task CSR_does_not_use_expected_Key_is_invalid()
    {
        var order = CreateOrder(
            new Identifier("dns", "example.th11s.de")
        );

        var privateKey = ECDsa.Create();

        order.Identifiers[0].Metadata[Identifier.MetadataKeys.PublicKey] = Convert.ToBase64String(privateKey.ExportSubjectPublicKeyInfo());

        order.Authorizations.Add(new(order, order.Identifiers[0], DateTimeOffset.Now.AddHours(1)) {
            Status = AuthorizationStatus.Valid,
        });

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDefaultSubjectSuffix()
           .WithCommonName("example.th11s.de")
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Order_exceeding_CSR_is_invalid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de"),
                new Identifier("dns", "too-much.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDefaultSubjectSuffix()
           .WithDnsName("example.th11s.de")
           .WithDnsName("test.th11s.de")
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CSR_exceeding_order_is_invalid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDnsName("example.th11s.de")
           .WithDnsName("test.th11s.de")
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CSR_without_CN_can_be_valid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDnsName("example.th11s.de")
           .WithDnsName("test.th11s.de")
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CSR_with_CN_that_is_no_SAN_is_valid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
            .WithDefaultSubjectSuffix()
            .WithCommonName("example.th11s.de")
            .WithCommonName("test.th11s.de")
            .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CSR_with_CN_that_is_also_SAN_is_valid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
            .WithDefaultSubjectSuffix()
            .WithCommonName("example.th11s.de")
            .WithCommonName("test.th11s.de")
            .WithDnsName("example.th11s.de")
            .WithDnsName("test.th11s.de")
            .AsBase64Url();

        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CSR_containing_more_SAN_is_invalid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDefaultSubjectSuffix()
           .WithDnsName("example.th11s.de")
           .WithDnsName("test.th11s.de")
           .WithDnsName("invalid.th11s.de")
           .AsBase64Url();

        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CSR_containing_more_CNs_is_invalid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDefaultSubjectSuffix()
           .WithCommonName("example.th11s.de")
           .WithCommonName("test.th11s.de")
           .WithCommonName("invalid.th11s.de")
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CSR_without_subject_but_all_SANs_is_valid()
    {
        var order = CreateOrder(
                new Identifier("dns", "example.th11s.de"),
                new Identifier("dns", "test.th11s.de")
            );

        order.CertificateSigningRequest = new CertificateRequestBuilder()
           .WithDnsName("example.th11s.de")
           .WithDnsName("test.th11s.de")
           .AsBase64Url();


        var sut = new CsrValidator(_profileConfiguration, NullLogger<CsrValidator>.Instance);
        var result = await sut.ValidateCsrAsync(order, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }
}

internal static class StringExtensions
{
    public static string AsBase64Url(this string input)
        => input.Replace("/", "_").Replace("+", "-").ReplaceLineEndings("").TrimEnd('=');

}