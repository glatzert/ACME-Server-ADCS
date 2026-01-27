using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;

namespace ACMEServer.Storage.FileSystem;

public abstract partial class StoreBase<T>(IOptions<FileStoreOptions> options)
        where T : class
{
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    protected IOptions<FileStoreOptions> Options { get; } = options;
    protected Regex IdentifierRegex { get; } = GetIdentifierRegex();

    protected async Task<T?> LoadFromPath(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            return null;

        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await LoadFromStream(fileStream, cancellationToken);
    }

    protected async Task<T?> LoadFromStream(
        FileStream fileStream, 
        CancellationToken cancellationToken)
    {
        if (fileStream.Length == 0)
            return null;

        fileStream.Seek(0, SeekOrigin.Begin);
        byte[] fileBytes = new byte[fileStream.Length];
        await fileStream.ReadExactlyAsync(fileBytes);

        ReadOnlySpan<byte> fileContent = new ReadOnlySpan<byte>(fileBytes);

        // Read past the UTF-8 BOM bytes if a BOM exists.
        if (fileBytes.StartsWith(Utf8Bom))
        {
            fileContent = fileContent.Slice(Utf8Bom.Length);
        }

        var reader = new Utf8JsonReader(fileContent);

        var result = Deserialize(ref reader);
        return result;
    }

    protected abstract T Deserialize(ref Utf8JsonReader reader);
    protected abstract void Serialize(Utf8JsonWriter writer, T content);

    protected async Task ReplaceFileStreamContent(FileStream fileStream, T content, CancellationToken cancellationToken)
    {
        if (fileStream.Length > 0)
            fileStream.SetLength(0);

        var writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions
        {
            Indented = true,
        });

        Serialize(writer, content);
        await writer.FlushAsync(cancellationToken);
    }

    protected static async Task ReplaceFileStreamContent(FileStream fileStream, string content, CancellationToken cancellationToken)
    {
        if (fileStream.Length > 0)
            fileStream.SetLength(0);

        byte[] utf8Bytes = Encoding.UTF8.GetBytes(content);

        await fileStream.WriteAsync(utf8Bytes, cancellationToken);
    }

    protected static void HandleVersioning(IVersioned? existingContent, IVersioned newContent)
    {
        if (existingContent != null && existingContent.Version != newContent.Version)
            throw new ConcurrencyException();

        newContent.Version = DateTime.UtcNow.Ticks;
    }

    [GeneratedRegex("[\\w\\d_-]+", RegexOptions.Compiled)]
    private static partial Regex GetIdentifierRegex ();
}
