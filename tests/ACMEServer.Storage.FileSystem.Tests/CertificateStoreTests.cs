using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace ACMEServer.Storage.FileSystem.Tests;

public class CertificateStoreTests : StoreTestBase
{
    [Fact]
    public async Task Saved_Certificates_Can_Be_Loaded()
    {
        var certBytes = new byte[2000];
        Random.Shared.NextBytes(certBytes);

        var certificates = new CertificateContainer(
            new(),
            new(),
            new(),
            certBytes,
            RevokationStatus.Revoked,
            Random.Shared.NextInt64()
        );

        var sut = new CertificateStore(new OptionsWrapper<FileStoreOptions>(Options));
        await sut.SaveCertificateAsync(certificates, CancellationToken.None);

        var loadedCertificate = await sut.LoadCertificateAsync(certificates.CertificateId, CancellationToken.None);

        Assert.NotNull(loadedCertificate);
        Assert.Equal(certificates.CertificateId, loadedCertificate.CertificateId);
        Assert.Equal(certificates.AccountId, loadedCertificate.AccountId);
        Assert.Equal(certificates.OrderId, loadedCertificate.OrderId);
        Assert.Equal(certificates.X509Certificates, loadedCertificate.X509Certificates);
        Assert.Equal(certificates.RevokationStatus, loadedCertificate.RevokationStatus);
        Assert.Equal(certificates.Version, loadedCertificate.Version);
    }

    [Theory,
        InlineData(CertificateJsonFileVariants.Certificate_SV1_FullModel)]
    public async Task Existing_File_Variants_Can_Be_Loaded(string certificateJson)
    {
        var certificateId = new CertificateId("Plt8mQd1xUeSoAa2ZartsA");
        var certificateFilePath = Path.Combine(Options.CertificateDirectory, $"{certificateId.Value}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(certificateFilePath)!);

        await File.WriteAllTextAsync(certificateFilePath, certificateJson, TestContext.Current.CancellationToken);

        var sut = new CertificateStore(new OptionsWrapper<FileStoreOptions>(Options));
        var loadedCertificate = await sut.LoadCertificateAsync(certificateId, TestContext.Current.CancellationToken);

        Assert.NotNull(loadedCertificate);
        Assert.Equal("Plt8mQd1xUeSoAa2ZartsA", loadedCertificate.CertificateId.Value);
        Assert.Equal("LZoGq5nxVUqdiNzk9RehFg", loadedCertificate.OrderId.Value);
        Assert.Equal("wTkk2r7Dj0WcMj5qCrHZdg", loadedCertificate.AccountId.Value);
    }
}

internal static class CertificateJsonFileVariants
{
    public const string Certificate_SV1_FullModel = """
    {
        "$id": "1",
        "SerializationVersion": 1,
        "CertificateId": "Plt8mQd1xUeSoAa2ZartsA",
        "OrderId": "LZoGq5nxVUqdiNzk9RehFg",
        "AccountId": "wTkk2r7Dj0WcMj5qCrHZdg",
        "X509Certificates": "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",
        "RevokationStatus": "Revoked",
        "Version": 639049666992203482
    }
    """;
}