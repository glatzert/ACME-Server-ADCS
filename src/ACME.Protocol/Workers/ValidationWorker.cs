using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Services;
using TGIT.ACME.Protocol.Storage;

namespace TGIT.ACME.Protocol.Workers
{
    public class ValidationWorker : IValidationWorker
    {
        private readonly IOrderStore _orderStore;
        private readonly IAccountStore _accountStore;
        private readonly IChallangeValidatorFactory _challangeValidatorFactory;

        public ValidationWorker(IOrderStore orderStore, IAccountStore accountStore,
            IChallangeValidatorFactory challangeValidatorFactory)
        {
            _orderStore = orderStore;
            _accountStore = accountStore;
            _challangeValidatorFactory = challangeValidatorFactory;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var orders = await _orderStore.GetValidatableOrders(cancellationToken);
            
            var tasks = new Task[orders.Count];
            for(int i = 0; i < orders.Count; ++i)
                tasks[i] = ValidateOrder(orders[i], cancellationToken);

            Task.WaitAll(tasks, cancellationToken);
        }

        private async Task ValidateOrder(Order order, CancellationToken cancellationToken)
        {
            var account = await _accountStore.LoadAccountAsync(order.AccountId, cancellationToken);
            if (account == null)
            {
                order.SetStatus(OrderStatus.Invalid);
                order.Error = new AcmeError("TODO", "Account could not be located. Order will be marked invalid.");
                await _orderStore.SaveOrderAsync(order, cancellationToken);

                return;
            }

            var pendingAuthZs = order.Authorizations.Where(a => a.Challenges.Any(c => c.Status == ChallengeStatus.Processing));

            foreach(var pendingAuthZ in pendingAuthZs)
            {
                if(pendingAuthZ.Expires <= DateTimeOffset.UtcNow)
                {
                    pendingAuthZ.ClearChallenges();
                    pendingAuthZ.SetStatus(AuthorizationStatus.Expired);
                    continue;
                }

                var challenge = pendingAuthZ.Challenges[0];

                var validator = _challangeValidatorFactory.GetValidator(challenge);
                var (isValid, error) = await validator.ValidateChallengeAsync(challenge, account, cancellationToken);

                if (isValid)
                {
                    challenge.SetStatus(ChallengeStatus.Valid);
                    pendingAuthZ.SetStatus(AuthorizationStatus.Valid);
                } else
                {
                    challenge.Error = error!;
                    challenge.SetStatus(ChallengeStatus.Invalid);
                    pendingAuthZ.SetStatus(AuthorizationStatus.Invalid);
                }
            }

            order.SetStatusFromAuthorizations();
            await _orderStore.SaveOrderAsync(order, cancellationToken);
        }
    }
}
