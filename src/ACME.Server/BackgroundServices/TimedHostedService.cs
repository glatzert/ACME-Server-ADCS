using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace TGIT.ACME.Server.BackgroundServices
{
    public abstract class TimedHostedService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<TimedHostedService> _logger;

        protected abstract bool EnableService { get; }
        protected abstract TimeSpan TimerInterval { get; }

        private Timer? _timer;
        private readonly SemaphoreSlim _interlock;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public TimedHostedService(IServiceProvider services, ILogger<TimedHostedService> logger)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger;

            _interlock = new SemaphoreSlim(1);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            if (EnableService)
            {
                _logger.LogInformation("Timed Hosted Service running.");
                _timer = new Timer(DoWorkCallback, null, TimeSpan.FromSeconds(15), TimerInterval);
            }

            return Task.CompletedTask;
        }

        protected async void DoWorkCallback(object? state)
        {
            if(! await _interlock.WaitAsync(TimerInterval / 2, _cancellationTokenSource.Token))
            {
                _logger.LogInformation("Waited half an execution time, but did not get execution lock.");
                return;
            }

            try
            {
                using var scopedServices = _services.CreateScope();
                await DoWork(scopedServices.ServiceProvider, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TimedHostedService failed with exception.");
            }
            finally {
                _interlock.Release();
            }
        }

        protected abstract Task DoWork(IServiceProvider services, CancellationToken cancellationToken);

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _cancellationTokenSource.Cancel();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _interlock?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
