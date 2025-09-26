using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;

namespace ACMEServer.Storage.FileSystem.Tests;

public class NonceStoreTests : StoreTestBase
{
    [Fact]
    public async Task Saving_a_Nonce_Creates_Nonce_File()
    {
        var nonce = new Nonce(GuidString.NewValue());

        var sut = new NonceStore(new OptionsWrapper<FileStoreOptions>(Options));
        await sut.SaveNonceAsync(nonce, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Options.NonceDirectory, nonce.Token)));
    }

    [Fact]
    public async Task Saved_Nonce_Can_Be_Consumed()
    {
        var nonce = new Nonce(GuidString.NewValue());

        var sut = new NonceStore(new OptionsWrapper<FileStoreOptions>(Options));
        await sut.SaveNonceAsync(nonce, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(Options.NonceDirectory, nonce.Token)));

        await sut.TryConsumeNonceAsync(nonce, CancellationToken.None);
        Assert.False(File.Exists(Path.Combine(Options.NonceDirectory, nonce.Token)));
    }
}
