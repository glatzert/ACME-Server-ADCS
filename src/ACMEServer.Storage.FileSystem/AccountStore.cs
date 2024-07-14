using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem
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
                using (var textStream = File.OpenText(accountLocatorPath))
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
    }
}
