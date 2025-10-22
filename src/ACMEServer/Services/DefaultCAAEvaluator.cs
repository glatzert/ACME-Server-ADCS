using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public class DefaultCAAEvaluator(
    ICAAQueryHandler caaQueryHandler, 
    IOptions<ACMEServerOptions> options,
    ILogger<DefaultCAAEvaluator> logger
    ) : ICAAEvaluator
{
    private readonly ICAAQueryHandler _caaQueryHandler = caaQueryHandler;
    private readonly IOptions<ACMEServerOptions> _options = options;
    private readonly ILogger<DefaultCAAEvaluator> _logger = logger;

    public Task<bool> HasValidCAARecord(Identifier identifier)
    {
        throw new NotImplementedException();
    }
}
