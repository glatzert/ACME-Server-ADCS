using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.IssuanceServices;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.Storage;

namespace TGIT.ACME.Protocol.Services
{
    public class DefaultOrderService : IOrderService
    {
        private readonly IOrderStore _orderStore;
        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly ICsrValidator _csrValidator;

        public DefaultOrderService(IOrderStore orderStore, IAuthorizationFactory authorizationFactory, ICsrValidator csrValidator)
        {
            _orderStore = orderStore;
            _authorizationFactory = authorizationFactory;
            _csrValidator = csrValidator;
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
            var order = await HandleLoadOrderAsync(account, orderId, OrderStatus.Valid, cancellationToken);

            return order.Certificate!;
        }

        public async Task<Order?> GetOrderAsync(Account account, string orderId, CancellationToken cancellationToken)
        {
            ValidateAccount(account);
            var order = await HandleLoadOrderAsync(account, orderId, null, cancellationToken);

            return order;
        }

        public async Task<Challenge> ProcessChallengeAsync(Account account, string orderId, string authId, string challengeId, CancellationToken cancellationToken)
        {
            ValidateAccount(account);
            var order = await HandleLoadOrderAsync(account, orderId, OrderStatus.Pending, cancellationToken);

            var authZ = order.GetAuthorization(authId);
            var challenge = authZ?.GetChallenge(challengeId);
            
            if (authZ == null || challenge == null)
                throw new NotFoundException();

            if (authZ.Status != AuthorizationStatus.Pending)
                throw new ConflictRequestException(AuthorizationStatus.Pending, authZ.Status);
            if (challenge.Status != ChallengeStatus.Pending)
                throw new ConflictRequestException(ChallengeStatus.Pending, challenge.Status);

            challenge.SetStatus(ChallengeStatus.Processing);
            authZ.SelectChallenge(challenge);

            await _orderStore.SaveOrderAsync(order, cancellationToken);

            return challenge;
        }

        public async Task<Order> ProcessCsr(Account account, string orderId, string? csr, CancellationToken cancellationToken)
        {
            ValidateAccount(account);
            var order = await HandleLoadOrderAsync(account, orderId, OrderStatus.Ready, cancellationToken);

            if (string.IsNullOrWhiteSpace(csr))
                throw new MalformedRequestException("CSR may not be empty.");

            var (isValid, error) = await _csrValidator.ValidateCsrAsync(order, csr, cancellationToken);

            if (isValid)
            {
                order.CertificateSigningRequest = csr;
                order.SetStatus(OrderStatus.Processing);
            } else
            {
                order.Error = error;
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

        private async Task<Order> HandleLoadOrderAsync(Account account, string orderId, OrderStatus? expectedStatus, CancellationToken cancellationToken)
        {
            var order = await _orderStore.LoadOrderAsync(orderId, cancellationToken);
            if (order == null)
                throw new NotFoundException();

            if (expectedStatus.HasValue && order.Status != expectedStatus)
                throw new ConflictRequestException(expectedStatus.Value, order.Status);

            if (order.AccountId != account.AccountId)
                throw new NotAllowedException();

            return order;
        }
    }
}
