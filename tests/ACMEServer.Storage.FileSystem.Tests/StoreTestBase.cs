using ACMEServer.Storage.FileSystem.Configuration;

namespace ACMEServer.Storage.FileSystem.Tests;

public abstract class StoreTestBase : IDisposable
{
    protected FileStoreOptions Options { get; }

    protected StoreTestBase()
    {
        Options = new FileStoreOptions()
        {
            BasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        };

        Directory.CreateDirectory(Options.BasePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(Options.BasePath))
        {
            Directory.Delete(Options.BasePath, true);
        }
    }
}