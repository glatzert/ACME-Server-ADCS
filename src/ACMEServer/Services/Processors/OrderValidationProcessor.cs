using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services.Processors;

public sealed class OrderValidationProcessor
{
    private readonly Channel<OrderId> _queue;
    
    private readonly TimeProvider _timeProvider;
    private readonly IServiceProvider _services;
    
    private readonly ILogger<OrderValidationProcessor> _logger;

    public OrderValidationProcessor(
        [FromKeyedServices(nameof(OrderValidationProcessor))] Channel<OrderId> queue,
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

    public async Task ProcessOrdersAsync(CancellationToken cancellationToken)
    {
        var canReadData = await _queue.Reader.WaitToReadAsync(cancellationToken);

        while (canReadData)
        {
            try
            {
                // When the reader is pulsed, we'll read all available data.
                // We'll create a scope here and process all orders currently in the queue.
                using var scope = _services.CreateScope();

                var accountStore = scope.ServiceProvider.GetRequiredService<IAccountStore>();
                var orderStore = scope.ServiceProvider.GetRequiredService<IOrderStore>();

                while (_queue.Reader.TryRead(out var orderId))
                {
                    _logger.LogInformation("Processing order {orderId}.", orderId);

                    var validationContext = await LoadAndValidateContextAsync(orderId, accountStore, orderStore, cancellationToken);
                    if (validationContext == null)
                    {
                        continue;
                    }

                    var challengeValidatorFactory = scope.ServiceProvider.GetRequiredService<IChallengeValidatorFactory>();
                    await ValidateOrder(validationContext, orderStore, challengeValidatorFactory, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orders for validation.");
            }

            canReadData = await _queue.Reader.WaitToReadAsync(cancellationToken);
        }
    }

    private async Task ValidateOrder(ValidationContext context, IOrderStore orderStore, IChallengeValidatorFactory challengeValidatorFactory, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to validate order {OrderId}.", context.Order.OrderId);

        // Find pending authorizations of the order
        var pendingAuthZs = context.Order.Authorizations.Where(a => a.Challenges.Any(c => c.Status == ChallengeStatus.Processing));
        foreach (var pendingAuthZ in pendingAuthZs)
        {
            // If an authorization has expired, mark it as such and continue to the next one
            if (pendingAuthZ.Expires <= _timeProvider.GetUtcNow())
            {
                pendingAuthZ.ClearChallenges();
                pendingAuthZ.SetStatus(AuthorizationStatus.Expired);
                continue;
            }

            // A pending authorization should have exactly one challenge
            var challenge = pendingAuthZ.Challenges.Single();
            _logger.LogInformation("Found pending authorization {AuthorizationId} with selected challenge {ChallengeId} ({ChallangeType})", pendingAuthZ.AuthorizationId, challenge.ChallengeId, challenge.Type);

            var validator = challengeValidatorFactory.GetValidator(challenge);
            var (challengeResult, error) = await validator.ValidateChallengeAsync(challenge, context.Account, cancellationToken);

            if (challengeResult == ChallengeResult.Valid)
            {
                _logger.LogInformation("Challenge {ChallengeId} ({ChallangeType}) was valid.", challenge.ChallengeId, challenge.Type);
                challenge.Validated = _timeProvider.GetUtcNow();
                challenge.SetStatus(ChallengeStatus.Valid);
                pendingAuthZ.SetStatus(AuthorizationStatus.Valid);
            }
            else
            {
                _logger.LogInformation("Challenge {ChallengeId} ({ChallangeType}) was invalid.", challenge.ChallengeId, challenge.Type);
                challenge.Error = error!;
                challenge.SetStatus(ChallengeStatus.Invalid);
                pendingAuthZ.SetStatus(AuthorizationStatus.Invalid);
            }
        }

        // This is idempotent, so we can call it every time, even if we validate the order multiple times
        context.Order.SetStatusFromAuthorizations();
        await orderStore.SaveOrderAsync(context.Order, cancellationToken);
    }

    private async Task<ValidationContext?> LoadAndValidateContextAsync(string orderId, IAccountStore accountStore, IOrderStore orderStore, CancellationToken cancellationToken)
    {
        var order = await orderStore.LoadOrderAsync(orderId, cancellationToken);

        // Check if the order exists and is in the correct state
        if (order == null)
        {
            _logger.LogWarning("Validation cannot be done, due to unkown order {OrderId}", orderId);
            return null;
        }
        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning("Validation cannot be done, due to order {OrderId} not being in a pending state", orderId);
            return null;
        }

        // Check if the account exists and is in the correct state
        var account = await accountStore.LoadAccountAsync(order.AccountId, cancellationToken);
        if (account == null || account.Status != AccountStatus.Valid)
        {
            if (account == null)
            {
                _logger.LogWarning("Validation cannot be done, due to unkown account {AccountId}", order.AccountId);
            }
            else
            {
                _logger.LogWarning("Validation cannot be done, due to account {AccountId} not being in a valid state", order.AccountId);
            }

            order.SetStatus(OrderStatus.Invalid);
            order.Error = new AcmeError("custom:accountInvalid", $"Account {order.AccountId} could not be located. Order {order.OrderId} will be marked invalid.");

            await orderStore.SaveOrderAsync(order, cancellationToken);
            return null;
        }

        return new ValidationContext
        {
            Order = order,
            Account = account
        };
    }

    private class ValidationContext
    {
        public required Order Order { get; init; }
        public required Account Account { get; init; }
    }
}
