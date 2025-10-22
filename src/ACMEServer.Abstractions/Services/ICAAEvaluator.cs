using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface ICAAEvaluator
{
    Task<bool> HasValidCAARecord(Identifier identifier);
}
