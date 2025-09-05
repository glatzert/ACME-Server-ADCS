using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services.Processors;

public sealed class CertificateIssuanceProcessor
{
    private readonly Channel<OrderId> _queue;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderValidationProcessor> _logger;

    public CertificateIssuanceProcessor(
        [FromKeyedServices(nameof(CertificateIssuanceProcessor))] Channel<OrderId> queue,
        TimeProvider timeProvider,
        IServiceProvider services,
        ILogger<OrderValidationProcessor> logger
        )
    {
        _queue = queue;
        _timeProvider = timeProvider;
        _services = services;
        _logger = logger;
    }

    public async Task ProcessCertificatesAsync(CancellationToken cancellationToken)
    {
        var canReadData = await _queue.Reader.WaitToReadAsync(cancellationToken);

        while (canReadData)
        {
            try
            {
                // When the reader is pulsed, we'll read all available data.
                // We'll create a scope here and process all orders currently in the queue.
                using var scope = _services.CreateScope();
                while (_queue.Reader.TryRead(out var orderId))
                {
                    _logger.LogInformation("Processing order {orderId}.", orderId);

                    var orderStore = scope.ServiceProvider.GetRequiredService<IOrderStore>();
                    var accountStore = scope.ServiceProvider.GetRequiredService<IAccountStore>();

                    var order = await LoadAndValidatOrderAsync(orderId, orderStore, accountStore, cancellationToken);
                    if (order == null || order.Status == OrderStatus.Valid)
                    {
                        continue;
                    }

                    var certificateIssuer = scope.ServiceProvider.GetRequiredService<ICertificateIssuer>();
                    await IssueCertificate(order, certificateIssuer, orderStore, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orders for validation.");
            }

            canReadData = await _queue.Reader.WaitToReadAsync(cancellationToken);
        }
    }

    private async Task<Order?> LoadAndValidatOrderAsync(OrderId orderId, IOrderStore orderStore, IAccountStore accountStore, CancellationToken cancellationToken)
    {
        var order = await orderStore.LoadOrderAsync(orderId, cancellationToken);

        // Check if the order exists and is in the correct state
        if (order == null)
        {
            _logger.LogWarning("Certificate cannot be issued, due to unkown order {OrderId}", orderId);
            return null;
        }
        if (order.Status != OrderStatus.Processing)
        {
            _logger.LogWarning("Certificate cannot be issued, due to order {OrderId} not being in processing state", orderId);
            return null;
        }

        // Check if the account exists and is in the correct state
        var account = await accountStore.LoadAccountAsync(order.AccountId, cancellationToken);
        if (account == null || account.Status != AccountStatus.Valid)
        {
            if (account == null)
            {
                _logger.LogWarning("Certificate cannot be issued, due to unkown account {AccountId}", order.AccountId);
            }
            else
            {
                _logger.LogWarning("Certificate cannot be issued, due to account {AccountId} not being in a valid state", order.AccountId);
            }

            order.SetStatus(OrderStatus.Invalid);
            order.Error = new AcmeError("custom:accountInvalid", $"Account {order.AccountId} could not be located. Order {order.OrderId} will be marked invalid.");

            await orderStore.SaveOrderAsync(order, cancellationToken);
            return null;
        }

        return order;
    }

    private async Task IssueCertificate(Order order, ICertificateIssuer certificateIssuer, IOrderStore orderStore, CancellationToken cancellationToken)
    {
        var (certificate, error) = await certificateIssuer.IssueCertificate(order.CertificateSigningRequest!, cancellationToken);

        if (certificate == null)
        {
            order.SetStatus(OrderStatus.Invalid);
            order.Error = error;
        }
        else if (certificate != null)
        {
            order.Certificate = certificate;

            var x509Certificate = new X509Certificate2(order.Certificate);

            order.Expires = x509Certificate.NotAfter;
            order.SetStatus(OrderStatus.Valid);
        }

        await orderStore.SaveOrderAsync(order, cancellationToken);
    }
}
