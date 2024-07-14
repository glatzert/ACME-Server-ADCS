using System.Collections.Generic;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Model.Services
{
    public interface IAuthorizationFactory
    {
        void CreateAuthorizations(Order order);
    }
}
