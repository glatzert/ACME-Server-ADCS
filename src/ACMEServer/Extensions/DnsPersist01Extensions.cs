using Th11s.ACMEServer.Configuration;

namespace Th11s.ACMEServer.Extensions;

public static class DnsPersist01Extensions
{
    extension(ACMEServerOptions acmeOptions)
    {
        public string[] GetDnsPersist01IssuerDomainNames()
        {
            if (acmeOptions.DnsPersistPreProvisioning?.IssuerDomainNames != null && acmeOptions.DnsPersistPreProvisioning.IssuerDomainNames.Count > 0)
            {
                return [.. acmeOptions.DnsPersistPreProvisioning.IssuerDomainNames];
            }
            else
            {
                return acmeOptions.CAAIdentities?.Take(10).ToArray() ?? [];
            }
        }
    }
}