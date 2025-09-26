using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem;

public class AccountStore : StoreBase, IAccountStore
{
    public AccountStore(IOptions<FileStoreOptions> options)
        : base(options)
    {
        Directory.CreateDirectory(Options.Value.AccountDirectory);
    }

    private string GetPath(AccountId accountId)
        => Path.Combine(Options.Value.AccountDirectory, accountId.Value, "account.json");

    public async Task<Account?> LoadAccountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId.Value) || !IdentifierRegex.IsMatch(accountId.Value))
            throw new MalformedRequestException("AccountId does not match expected format.");

        var accountPath = GetPath(accountId);

        var account = await LoadFromPath<Account>(accountPath, cancellationToken);
        return account;
    }

    public async Task SaveAccountAsync(Account account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(account);

        var accountPath = GetPath(account.AccountId);
        Directory.CreateDirectory(Path.GetDirectoryName(accountPath));

        using (var fileStream = File.Open(accountPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            var existingAccount = await LoadFromStream<Account>(fileStream, cancellationToken);
            HandleVersioning(existingAccount, account);

            await ReplaceFileStreamContent(fileStream, account, cancellationToken);
        }

        // Unfortunately this needs special permissions on Windows, so we won't use it now.
        // File.CreateSymbolicLink(
        //    Path.Combine(Options.Value.AccountDirectory, account.Jwk.KeyHash),
        //    accountPath
        // );


        var accountLocatorPath = Path.Combine(Options.Value.AccountDirectory, account.Jwk.KeyHash);
        using (var fileStream = File.Open(accountLocatorPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
        {
            await ReplaceFileStreamContent(fileStream, account.AccountId.Value, cancellationToken);
        }
    }

    public async Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
    {
        try
        {
            var accountLocatorPath = Path.Combine(Options.Value.AccountDirectory, jwk.KeyHash);
            using (var textStream = File.OpenText(accountLocatorPath))
            {
                var accountId = await textStream.ReadToEndAsync();
                return await LoadAccountAsync(new(accountId), cancellationToken);
            }
        }
        catch
        {
            return null;
        }
    }

    public Task<List<OrderId>> GetAccountOrders(AccountId accountId, CancellationToken cancellationToken)
    {
        var ownerDirectory = Path.Combine(Options.Value.AccountDirectory, accountId.Value, "orders");
        var directory = new DirectoryInfo(ownerDirectory);
        var orderFiles = directory.EnumerateFiles();

        return Task.FromResult(orderFiles.Select(x => new OrderId(x.Name)).ToList());
    }
}
