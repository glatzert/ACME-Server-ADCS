using System.Collections.Generic;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public interface IAuthorizationFactory
    {
        void CreateAuthorizations(Order order);
    }
}
