using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
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
        }
    }
}
