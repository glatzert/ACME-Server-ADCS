using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;
using Th11s.ACMEServer.Services.Processors;

namespace Th11s.ACMEServer.Services
{
    public class DefaultOrderService : IOrderService
    {
        private readonly IOrderStore _orderStore;
        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly ICSRValidator _csrValidator;
        private readonly Channel<OrderId> _validationQueue;

        public DefaultOrderService(
            IOrderStore orderStore, 
            IAuthorizationFactory authorizationFactory, 
            ICSRValidator csrValidator,
            [FromKeyedServices(nameof(OrderValidationProcessor))] Channel<OrderId> validationQueue)
        {
            _orderStore = orderStore;
            _authorizationFactory = authorizationFactory;
            _csrValidator = csrValidator;
            _validationQueue = validationQueue;
        }

        public async Task<Order> CreateOrderAsync(Account account,
            IEnumerable<Identifier> identifiers,
            DateTimeOffset? notBefore, DateTimeOffset? notAfter,
            CancellationToken cancellationToken)
        {
            ValidateAccount(account);

            var order = new Order(account, identifiers)
            {
                NotBefore = notBefore,
                NotAfter = notAfter
            };

            _authorizationFactory.CreateAuthorizations(order);

            await _orderStore.SaveOrderAsync(order, cancellationToken);

            return order;
        }

        public async Task<byte[]> GetCertificate(Account account, string orderId, CancellationToken cancellationToken)
        {
            ValidateAccount(account);
            var order = await HandleLoadOrderAsync(account, orderId, cancellationToken);
            if (order.Status != OrderStatus.Valid)
            {
                throw new ConflictRequestException(OrderStatus.Valid, order.Status);
            }

            return order.Certificate!;
        }

        public async Task<Order?> GetOrderAsync(Account account, string orderId, CancellationToken cancellationToken)
        {
            ValidateAccount(account);
            var order = await HandleLoadOrderAsync(account, orderId, cancellationToken);

            return order;
        }

        public async Task<Challenge> ProcessChallengeAsync(Account account, string orderId, string authId, string challengeId, CancellationToken cancellationToken)
        {
            ValidateAccount(account);
            var order = await HandleLoadOrderAsync(account, orderId, cancellationToken);

            var authZ = order.GetAuthorization(authId);
            var challenge = authZ?.GetChallenge(challengeId);

            if (authZ == null || challenge == null)
                throw new NotFoundException();

            // If the challenge exists AND is not pending, we return it,
            // since some clients are not RFC complaint and poll on the challenge
            if (challenge.Status != ChallengeStatus.Pending)
                return challenge;


            if (order.Status != OrderStatus.Pending)
                throw new ConflictRequestException(OrderStatus.Pending, order.Status);
            if (authZ.Status != AuthorizationStatus.Pending)
                throw new ConflictRequestException(AuthorizationStatus.Pending, authZ.Status);


            challenge.SetStatus(ChallengeStatus.Processing);
            authZ.SelectChallenge(challenge);

            await _orderStore.SaveOrderAsync(order, cancellationToken);
            _validationQueue.Writer.TryWrite(new(order.OrderId));

            return challenge;
        }

        public async Task<Order> ProcessCsr(Account account, string orderId, string? csr, CancellationToken cancellationToken)
        {
            ValidateAccount(account);
            var order = await HandleLoadOrderAsync(account, orderId, cancellationToken);
            
            if(order.Status != OrderStatus.Ready)
            {
                // This is not defined in the specs, but some clients resubmit the csr while waiting.
                // We'll return the current order, if the csr did not change.
                if(order.Status == OrderStatus.Processing || order.Status == OrderStatus.Valid)
                {
                    if(csr == order.CertificateSigningRequest)
                    {
                        return order;
                    }

                    throw new ConflictRequestException("The order was alread 'processing' or 'valid' and the client tried to submit another csr.");
                }

                throw new ConflictRequestException(OrderStatus.Ready, order.Status);
            }

            if (string.IsNullOrWhiteSpace(csr))
                throw new MalformedRequestException("CSR may not be empty.");

            var validationResult = await _csrValidator.ValidateCsrAsync(order, csr, cancellationToken);

            if (validationResult.IsValid)
            {
                order.CertificateSigningRequest = csr;
                order.SetStatus(OrderStatus.Processing);
            }
            else
            {
                order.Error = validationResult.Error;
                order.SetStatus(OrderStatus.Invalid);
            }

            await _orderStore.SaveOrderAsync(order, cancellationToken);
            return order;
        }

        private static void ValidateAccount(Account? account)
        {
            if (account == null)
                throw new NotAllowedException();

            if (account.Status != AccountStatus.Valid)
                throw new ConflictRequestException(AccountStatus.Valid, account.Status);
        }

        private async Task<Order> HandleLoadOrderAsync(Account account, string orderId, CancellationToken cancellationToken)
        {
            var order = await _orderStore.LoadOrderAsync(orderId, cancellationToken);
            if (order == null)
                throw new NotFoundException();

            if (order.AccountId != account.AccountId)
                throw new NotAllowedException();

            return order;
        }
    }
}
