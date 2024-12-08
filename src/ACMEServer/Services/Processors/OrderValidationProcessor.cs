using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services.Processors
{
    internal class OrderValidationProcessor
    {
        private readonly Channel<string> _orderQueue;
        private readonly CancellationTokenSource _cancellationTokenSource = new ();

        private readonly TimeProvider _timeProvider;
        private readonly IAccountStore _accountStore;
        private readonly IOrderStore _orderStore;
        
        private readonly IChallengeValidatorFactory _challengeValidatorFactory;
        
        private readonly ILogger<OrderValidationProcessor> _logger;

        public OrderValidationProcessor(
            [FromKeyedServices(nameof(OrderValidationProcessor))] Channel<string> orderQueue,
            TimeProvider timeProvider,
            IAccountStore accountStore,
            IOrderStore orderStore,
            IChallengeValidatorFactory challengeValidatorFactory,
            ILogger<OrderValidationProcessor> logger
            )
        {
            _orderQueue = orderQueue;
            _timeProvider = timeProvider;

            _accountStore = accountStore;
            _orderStore = orderStore;

            _logger = logger;

            _ = ProcessOrdersAsync(_cancellationTokenSource.Token);
            _challengeValidatorFactory = challengeValidatorFactory;
        }

        private async Task ProcessOrdersAsync(CancellationToken cancellationToken)
        {
            await foreach (var orderId in _orderQueue.Reader.ReadAllAsync(cancellationToken))
            {
                _logger.LogInformation("Processing order {orderId}.", orderId);

                var validationContext = await LoadAndValidateContextAsync(orderId, cancellationToken);
                if (validationContext == null)
                {
                    continue;
                }

                await ValidateOrder(validationContext, cancellationToken);
            }
        }

        private async Task ValidateOrder(ValidationContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Attempting to validate order {context.Order.OrderId}.");

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

                var validator = _challengeValidatorFactory.GetValidator(challenge);
                var (challengeResult, error) = await validator.ValidateChallengeAsync(challenge, context.Account, cancellationToken);

                if (challengeResult == ChallengeResult.Valid)
                {
                    _logger.LogInformation($"Challenge  {{ChallengeId}} ({{ChallangeType}}) was valid.", challenge.ChallengeId, challenge.Type);
                    challenge.Validated = _timeProvider.GetUtcNow();
                    challenge.SetStatus(ChallengeStatus.Valid);
                    pendingAuthZ.SetStatus(AuthorizationStatus.Valid);
                }
                else
                {
                    _logger.LogInformation($"Challenge  {{ChallengeId}} ({{ChallangeType}}) was invalid.", challenge.ChallengeId, challenge.Type);
                    challenge.Error = error!;
                    challenge.SetStatus(ChallengeStatus.Invalid);
                    pendingAuthZ.SetStatus(AuthorizationStatus.Invalid);
                }
            }

            context.Order.SetStatusFromAuthorizations();
            await _orderStore.SaveOrderAsync(context.Order, cancellationToken);
        }

        private async Task<ValidationContext?> LoadAndValidateContextAsync(string orderId, CancellationToken cancellationToken)
        {
            var order = await _orderStore.LoadOrderAsync(orderId, cancellationToken);

            // Check if the order exists and is in the correct state
            if (order == null)
            {
                _logger.LogWarning($"Validation cannot be done, due to unkown order {orderId}");
                return null;
            }
            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning($"Validation cannot be done, due to order {orderId} not being in a pending state");
                return null;
            }

            // Check if the account exists and is in the correct state
            var account = await _accountStore.LoadAccountAsync(order.AccountId, cancellationToken);
            if (account == null || account.Status != AccountStatus.Valid)
            {
                if (account == null)
                {
                    _logger.LogWarning($"Validation cannot be done, due to unkown account {order.AccountId}");
                }
                else
                {
                    _logger.LogWarning($"Validation cannot be done, due to account {order.AccountId} not being in a valid state");
                }

                order.SetStatus(OrderStatus.Invalid);
                order.Error = new AcmeError("custom:accountInvalid", "Account could not be located. Order will be marked invalid.");

                await _orderStore.SaveOrderAsync(order, cancellationToken);
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
}
