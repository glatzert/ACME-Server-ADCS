
using ACMEServer.Storage.FileSystem.Configuration;

namespace Th11s.ACMEServer.ConfigCLI;

internal class FileStoreConfigBuilder
{
    public FileStoreOptions Options { get; } = new();
    public Status Status =>
        string.IsNullOrWhiteSpace(Options.BasePath)
            ? Status.NeedsAttention
            : Status.AllGood;

    internal void SetBasePath(string? directoryPath)
        => Options.BasePath = directoryPath;
}
