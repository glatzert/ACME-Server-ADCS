using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Storage;
using TGIT.ACME.Storage.FileStore.Configuration;

namespace TGIT.ACME.Storage.FileStore
{
    public class OrderStore : StoreBase, IOrderStore
    {
        private readonly ILogger<OrderStore> _logger;

        public OrderStore(IOptions<FileStoreOptions> options, ILogger<OrderStore> logger)
            : base(options)
        {
            _logger = logger;
            Directory.CreateDirectory(Options.Value.OrderPath);
        }

        private string GetOrderPath(string orderId)
            => Path.Combine(Options.Value.OrderPath, $"{orderId}.json");

        public async Task<Order?> LoadOrderAsync(string orderId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(orderId) || !IdentifierRegex.IsMatch(orderId))
                throw new MalformedRequestException("OrderId does not match expected format.");

            var orderFilePath = GetOrderPath(orderId);

            var order = await LoadFromPath<Order>(orderFilePath, cancellationToken);
            return order;
        }

        public async Task SaveOrderAsync(Order setOrder, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (setOrder is null)
                throw new ArgumentNullException(nameof(setOrder));

            var orderFilePath = GetOrderPath(setOrder.OrderId);

            Directory.CreateDirectory(Path.GetDirectoryName(orderFilePath));

            await CreateOwnerFileAsync(setOrder, cancellationToken);
            await WriteWorkFilesAsync(setOrder, cancellationToken);

            using (var fileStream = File.Open(orderFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                var existingOrder = await LoadFromStream<Order>(fileStream, cancellationToken);

                HandleVersioning(existingOrder, setOrder);
                await ReplaceFileStreamContent(fileStream, setOrder, cancellationToken);
            }
        }

        private async Task CreateOwnerFileAsync(Order order, CancellationToken cancellationToken)
        {
            var ownerDirectory = Path.Combine(Options.Value.AccountPath, order.AccountId, "orders");
            Directory.CreateDirectory(ownerDirectory);

            var ownerFilePath = Path.Combine(ownerDirectory, order.OrderId);
            if (!File.Exists(ownerFilePath)) {
                await File.WriteAllTextAsync(ownerFilePath, 
                    order.Expires?.ToString("o", CultureInfo.InvariantCulture), 
                    cancellationToken);
            }
        }

        private async Task WriteWorkFilesAsync(Order order, CancellationToken cancellationToken)
        {
            var validationDirectory = Path.Combine(Options.Value.WorkingPath, "validate");
            Directory.CreateDirectory(validationDirectory);

            var validationFilePath = Path.Combine(validationDirectory, order.OrderId);
            if (order.Authorizations.Any(a => a.Challenges.Any(c => c.Status == ChallengeStatus.Processing)))
            {
                if (!File.Exists(validationFilePath)) {
                    await File.WriteAllTextAsync(validationFilePath, 
                        order.Authorizations.Min(a => a.Expires).ToString("o", CultureInfo.InvariantCulture),
                        cancellationToken);
                }
            } 
            else if (File.Exists(validationFilePath))
            {
                File.Delete(validationFilePath);
            }

            var processDirectory = Path.Combine(Options.Value.WorkingPath!, "process");
            Directory.CreateDirectory(processDirectory);

            var processingFilePath = Path.Combine(processDirectory, order.OrderId);
            if(order.Status == OrderStatus.Processing)
            {
                if (!File.Exists(processingFilePath)) {
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

            var workPath = Path.Combine(Options.Value.WorkingPath, "validate");
            if (!Directory.Exists(workPath))
                return result;

            var files = Directory.EnumerateFiles(workPath);
            foreach(var filePath in files)
            {
                try
                {
                    var orderId = Path.GetFileName(filePath);
                    var order = await LoadOrderAsync(orderId, cancellationToken);

                    if(order != null)
                        result.Add(order);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Could not load validatable orders.");
                }
            }

            return result;
        }

        public async Task<List<Order>> GetFinalizableOrders(CancellationToken cancellationToken)
        {
            var result = new List<Order>();

            var workPath = Path.Combine(Options.Value.WorkingPath, "process");
            if (!Directory.Exists(workPath))
                return result;

            var files = Directory.EnumerateFiles(workPath);
            foreach (var filePath in files)
            {
                try
                {
                    var orderId = Path.GetFileName(filePath);
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
}
