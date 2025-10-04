using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem;

public class OrderStore : StoreBase, IOrderStore
{
    private readonly ILogger<OrderStore> _logger;

    public OrderStore(IOptions<FileStoreOptions> options, ILogger<OrderStore> logger)
        : base(options)
    {
        _logger = logger;
        Directory.CreateDirectory(Options.Value.OrderDirectory);
    }

    private string GetOrderPath(OrderId orderId)
        => Path.Combine(Options.Value.OrderDirectory, $"{orderId.Value}.json");

    public async Task<Order?> LoadOrderAsync(OrderId orderId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId.Value) || !IdentifierRegex.IsMatch(orderId.Value))
            throw new MalformedRequestException("OrderId does not match expected format.");

        var orderFilePath = GetOrderPath(orderId);

        var order = await LoadFromPath<Order>(orderFilePath, cancellationToken);
        return order;
    }

    public async Task SaveOrderAsync(Order order, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(order);

        var orderFilePath = GetOrderPath(order.OrderId);

        Directory.CreateDirectory(Path.GetDirectoryName(orderFilePath)!);

        await CreateOwnerFileAsync(order, cancellationToken);
        await WriteWorkFilesAsync(order, cancellationToken);

        using var fileStream = File.Open(orderFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        var existingOrder = await LoadFromStream<Order>(fileStream, cancellationToken);

        HandleVersioning(existingOrder, order);
        await ReplaceFileStreamContent(fileStream, order, cancellationToken);
    }

    private async Task CreateOwnerFileAsync(Order order, CancellationToken cancellationToken)
    {
        var ownerDirectory = Path.Combine(Options.Value.AccountDirectory, order.AccountId.Value, "orders");
        Directory.CreateDirectory(ownerDirectory);

        var ownerFilePath = Path.Combine(ownerDirectory, order.OrderId.Value);
        if (!File.Exists(ownerFilePath))
        {
            await File.WriteAllTextAsync(ownerFilePath,
                order.Expires?.ToString("o", CultureInfo.InvariantCulture),
                cancellationToken);
        }
    }

    private async Task WriteWorkFilesAsync(Order order, CancellationToken cancellationToken)
    {
        var validationDirectory = Path.Combine(Options.Value.WorkingDirectory, "validate");
        Directory.CreateDirectory(validationDirectory);

        var validationFilePath = Path.Combine(validationDirectory, order.OrderId.Value);
        if (order.Authorizations.Any(a => a.Challenges.Any(c => c.Status == ChallengeStatus.Processing)))
        {
            if (!File.Exists(validationFilePath))
            {
                await File.WriteAllTextAsync(validationFilePath,
                    order.Authorizations.Min(a => a.Expires).ToString("o", CultureInfo.InvariantCulture),
                    cancellationToken);
            }
        }
        else if (File.Exists(validationFilePath))
        {
            File.Delete(validationFilePath);
        }

        var processDirectory = Path.Combine(Options.Value.WorkingDirectory!, "process");
        Directory.CreateDirectory(processDirectory);

        var processingFilePath = Path.Combine(processDirectory, order.OrderId.Value);
        if (order.Status == OrderStatus.Processing)
        {
            if (!File.Exists(processingFilePath))
            {
                await File.WriteAllTextAsync(processingFilePath,
                    order.Expires?.ToString("o", CultureInfo.InvariantCulture),
                    cancellationToken);
            }
        }
        else if (File.Exists(processingFilePath))
        {
            File.Delete(processingFilePath);
        }
    }

    public async Task<List<Order>> GetValidatableOrders(CancellationToken cancellationToken)
    {
        var result = new List<Order>();

        var workPath = Path.Combine(Options.Value.WorkingDirectory, "validate");
        if (!Directory.Exists(workPath))
            return result;

        var files = Directory.EnumerateFiles(workPath);
        foreach (var filePath in files)
        {
            try
            {
                var orderId = new OrderId(Path.GetFileName(filePath));
                var order = await LoadOrderAsync(orderId, cancellationToken);

                if (order != null)
                    result.Add(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load validatable orders.");
            }
        }

        return result;
    }

    public async Task<List<Order>> GetFinalizableOrders(CancellationToken cancellationToken)
    {
        var result = new List<Order>();

        var workPath = Path.Combine(Options.Value.WorkingDirectory, "process");
        if (!Directory.Exists(workPath))
            return result;

        var files = Directory.EnumerateFiles(workPath);
        foreach (var filePath in files)
        {
            try
            {
                var orderId = new OrderId(Path.GetFileName(filePath));
                var order = await LoadOrderAsync(orderId, cancellationToken);

                if (order != null)
                    result.Add(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load finalizable orders.");
            }
        }

        return result;
    }
}
