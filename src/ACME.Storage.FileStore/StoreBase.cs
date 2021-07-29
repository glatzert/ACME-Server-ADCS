using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Storage.FileStore.Configuration;

namespace TGIT.ACME.Storage.FileStore
{
    public class StoreBase
    {
        protected IOptions<FileStoreOptions> Options { get; }
        protected Regex IdentifierRegex { get; }

        public StoreBase(IOptions<FileStoreOptions> options)
        {
            Options = options;
            IdentifierRegex = new Regex("[\\w\\d_-]+", RegexOptions.Compiled);
        }

        protected static async Task<T?> LoadFromPath<T>(string filePath, CancellationToken cancellationToken)
            where T : class
        {
            if (!File.Exists(filePath))
                return null;

            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await LoadFromStream<T>(fileStream, cancellationToken);
            }
        }

        protected static async Task<T?> LoadFromStream<T>(FileStream fileStream, CancellationToken cancellationToken) 
            where T : class
        {
            if (fileStream.Length == 0)
                return null;

            fileStream.Seek(0, SeekOrigin.Begin);

            var utf8Bytes = new byte[fileStream.Length];
            await fileStream.ReadAsync(utf8Bytes, cancellationToken);
            var result = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(utf8Bytes), JsonDefaults.Settings);

            return result;
        }

        protected static async Task ReplaceFileStreamContent<T>(FileStream fileStream, T content, CancellationToken cancellationToken)
        {
            if (fileStream.Length > 0)
                fileStream.SetLength(0);

            var utf8Bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content, JsonDefaults.Settings));
            await fileStream.WriteAsync(utf8Bytes, cancellationToken);
        }

        protected static void HandleVersioning(IVersioned? existingContent, IVersioned newContent)
        {
            if (existingContent != null && existingContent.Version != newContent.Version)
                throw new ConcurrencyException();

            newContent.Version = DateTime.UtcNow.Ticks;
        }
    }
}
