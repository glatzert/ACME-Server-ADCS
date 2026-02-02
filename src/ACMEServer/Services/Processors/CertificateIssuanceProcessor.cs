using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services.Processors;

public sealed class CertificateIssuanceProcessor(
    CertificateIssuanceQueue queue,
    IServiceProvider services,
    ILoggerFactory loggerFactory
    )
{
    private readonly CertificateIssuanceQueue _queue = queue;
    private readonly IServiceProvider _services = services;
    private readonly ILogger<CertificateIssuanceProcessor> _logger = loggerFactory.CreateLogger<CertificateIssuanceProcessor>();
    private readonly ILogger _issuanceLogger = loggerFactory.CreateLogger("Th11s.ACMEServer.IssuedCertificates");

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
                    _logger.ProcessingOrderForIssuance(orderId);

                    var certificateStore = scope.ServiceProvider.GetRequiredService<ICertificateStore>();
                    var orderStore = scope.ServiceProvider.GetRequiredService<IOrderStore>();
                    var accountStore = scope.ServiceProvider.GetRequiredService<IAccountStore>();

                    var order = await LoadAndValidatOrderAsync(orderId, orderStore, accountStore, cancellationToken);
                    if (order == null || order.Status == OrderStatus.Valid)
                    {
                        continue;
                    }

                    var certificateIssuer = scope.ServiceProvider.GetRequiredService<ICertificateIssuer>();
                    await IssueCertificate(order, certificateIssuer, orderStore, certificateStore, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorProcessingOrdersForValidation(ex);
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
            _logger.UnknownOrderForIssuance(orderId);
            return null;
        }
        if (order.Status != OrderStatus.Processing)
        {
            _logger.OrderNotInProcessingState(orderId);
            return null;
        }

        // Check if the account exists and is in the correct state
        var account = await accountStore.LoadAccountAsync(order.AccountId, cancellationToken);
        if (account == null || account.Status != AccountStatus.Valid)
        {
            if (account == null)
            {
                _logger.UnknownAccountForIssuance(order.AccountId);
            }
            else
            {
                _logger.AccountNotValidForIssuance(order.AccountId);
            }

            order.SetStatus(OrderStatus.Invalid);
            order.Error = new AcmeError("custom:accountInvalid", $"Account {order.AccountId} could not be located. Order {order.OrderId} will be marked invalid.");

            await orderStore.SaveOrderAsync(order, cancellationToken);
            return null;
        }

        return order;
    }

    private async Task IssueCertificate(Order order, ICertificateIssuer certificateIssuer, IOrderStore orderStore, ICertificateStore certificateStore, CancellationToken cancellationToken)
    {
        var (x509Certificates, error) = await certificateIssuer.IssueCertificateAsync(order.Profile, order.CertificateSigningRequest!, cancellationToken);

        if (x509Certificates == null)
        {
            order.SetStatus(OrderStatus.Invalid);
            order.Error = error;
        }
        else
        {
            var certificates = new CertificateContainer(order.AccountId, order.OrderId, x509Certificates);
            await certificateStore.SaveCertificateAsync(certificates, cancellationToken);

            order.CertificateId = certificates.CertificateId;
            order.SetStatus(OrderStatus.Valid);

            var issuedCertificate = x509Certificates.GetLeafCertificate();
            // TODO: include SANS?
            _issuanceLogger.CertificateIssuedForOrder(
                order.OrderId,
                issuedCertificate.Thumbprint,
                issuedCertificate.SerialNumber,
                $"\r\n{string.Join(",\r\n", order.Identifiers.Select(x => x.ToString()))}");

            order.Expires = issuedCertificate.NotAfter;
        }
        
        await orderStore.SaveOrderAsync(order, cancellationToken);
    }
}
