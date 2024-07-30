using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Storage;
using TGIT.ACME.Storage.FileStore.Configuration;

namespace TGIT.ACME.Storage.FileStore
{
    public class AccountStore : StoreBase, IAccountStore
    {
        public AccountStore(IOptions<FileStoreOptions> options)
            : base(options)
        {
            Directory.CreateDirectory(Options.Value.AccountPath);
        }

        private string GetPath(string accountId)
            => Path.Combine(Options.Value.AccountPath, accountId, "account.json");

        public async Task<Account?> LoadAccountAsync(string accountId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accountId) || !IdentifierRegex.IsMatch(accountId))
                throw new MalformedRequestException("AccountId does not match expected format.");

            var accountPath = GetPath(accountId);

            var account = await LoadFromPath<Account>(accountPath, cancellationToken);
            return account;
        }

        public async Task SaveAccountAsync(Account setAccount, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (setAccount is null)
                throw new ArgumentNullException(nameof(setAccount));

            var accountPath = GetPath(setAccount.AccountId);
            Directory.CreateDirectory(Path.GetDirectoryName(accountPath));

            using (var fileStream = File.Open(accountPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                var existingAccount = await LoadFromStream<Account>(fileStream, cancellationToken);
                HandleVersioning(existingAccount, setAccount);

                await ReplaceFileStreamContent(fileStream, setAccount, cancellationToken);
            }

            var accountLocatorPath = Path.Combine(Options.Value.AccountPath, setAccount.Jwk.KeyHash);
            using (var fileStream = File.Open(accountLocatorPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                await ReplaceFileStreamContent(fileStream, setAccount.AccountId, cancellationToken);
            }
        }

        public async Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
        {
            try
            {
                var accountLocatorPath = Path.Combine(Options.Value.AccountPath, jwk.KeyHash);
                using(var textStream = File.OpenText(accountLocatorPath))
                {
                    var accountId = await textStream.ReadToEndAsync();
                    return await LoadAccountAsync(accountId, cancellationToken);
                }
            }
            catch
            {
                return null;
            }
        }

        public Task<List<string>> GetAccountOrders(string accountId, CancellationToken cancellationToken)
        {
            var ownerDirectory = Path.Combine(Options.Value.AccountPath, accountId, "orders");
            var directory = new DirectoryInfo(ownerDirectory);
            var orderFiles = directory.EnumerateFiles();

            return Task.FromResult(orderFiles.Select(x => x.Name).ToList());
        }
    }
}
