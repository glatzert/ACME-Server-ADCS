using System.Threading;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;
using Th11s.ACMEServer.Model.Workers;

namespace Th11s.ACMEServer.BackgroundServices.Workers
{
    public class IssuanceWorker : IIssuanceWorker
    {
        private readonly IOrderStore _orderStore;
        private readonly ICertificateIssuer _issuer;

        public IssuanceWorker(IOrderStore orderStore, ICertificateIssuer issuer)
        {
            _orderStore = orderStore;
            _issuer = issuer;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var orders = await _orderStore.GetFinalizableOrders(cancellationToken);

            var tasks = new Task[orders.Count];
            for (var i = 0; i < orders.Count; ++i)
                tasks[i] = IssueCertificate(orders[i], cancellationToken);

            Task.WaitAll(tasks, cancellationToken);
        }

        private async Task IssueCertificate(Order order, CancellationToken cancellationToken)
        {
            var (certificate, error) = await _issuer.IssueCertificate(order.CertificateSigningRequest!, cancellationToken);

            if (certificate == null)
            {
                order.SetStatus(OrderStatus.Invalid);
                order.Error = error;
            }
            else if (certificate != null)
            {
                order.Certificate = certificate;
                order.SetStatus(OrderStatus.Valid);
            }

            await _orderStore.SaveOrderAsync(order, cancellationToken);
        }
    }
}
