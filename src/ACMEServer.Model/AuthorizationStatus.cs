using System.Linq;

namespace TGIT.ACME.Protocol.Model
{
    public enum AuthorizationStatus
    {
        Pending,
        Valid,
        Invalid,
        Revoked,
        Deactivated,
        Expired
    }
}
