using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Storage;
using TGIT.ACME.Protocol.Workers;
using TGIT.ACME.Server.Configuration;

namespace TGIT.ACME.Server.BackgroundServices
{

    public class HostedValidationService : TimedHostedService
    {
        private readonly IOptions<ACMEServerOptions> _options;

        public HostedValidationService(IOptions<ACMEServerOptions> options, 
            IServiceProvider services, ILogger<TimedHostedService> logger) 
            : base(services, logger)
        {
            _options = options;
        }

        protected override bool EnableService => _options.Value.HostedWorkers?.EnableValidationService == true;
        protected override TimeSpan TimerInterval => TimeSpan.FromSeconds(_options.Value.HostedWorkers!.ValidationCheckInterval);


        protected override async Task DoWork(IServiceProvider services, CancellationToken cancellationToken)
        {
            var validationWorker = services.GetRequiredService<IValidationWorker>();
            await validationWorker.RunAsync(cancellationToken);
        }
    }
}
