using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public interface ILinkGenerator
{
    string NewNonce();

    string NewAccount();
    string GetAccount(AccountId accountId);
    string KeyChange();
    string GetOrderList(AccountId accountId);


    string NewOrder();
    string GetOrder(OrderId order);
    string GetAuthorization(OrderId orderId, AuthorizationId authorizationId);
    string GetChallenge(OrderId orderId, AuthorizationId authorizationId, ChallengeId challengeId);

    string FinalizeOrder(OrderId order);
    string GetCertificate(OrderId order);

    string RevokeCert();

    string ProfileMetadata(ProfileName profileName);
}
