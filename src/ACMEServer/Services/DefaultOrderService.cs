using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services
{
    public class DefaultOrderService : IOrderService
    {
        private readonly IOrderStore _orderStore;
        private readonly IAuthorizationFactory _authorizationFactory;
        private readonly ICSRValidator _csrValidator;

        public DefaultOrderService(IOrderStore orderStore, IAuthorizationFactory authorizationFactory, ICSRValidator csrValidator)
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
            var order = await HandleLoadOrderAsync(account, orderId, null, cancellationToken);

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
