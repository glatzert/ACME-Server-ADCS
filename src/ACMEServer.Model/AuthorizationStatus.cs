namespace Th11s.ACMEServer.Model;

public enum AuthorizationStatus
{
    Pending,
    Valid,
    Invalid,
    Revoked,
    Deactivated,
    Expired
}
