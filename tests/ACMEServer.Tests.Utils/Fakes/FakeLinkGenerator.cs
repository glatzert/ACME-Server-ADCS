using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Tests.Utils.Fakes
{
    public class FakeLinkGenerator : ILinkGenerator
    {
        private const string _baseUrl = "https://acme.th11s.de";


        public string AccountHash() => $"{_baseUrl}/account-hash";
        public string FinalizeOrder(OrderId order) => $"{_baseUrl}/order/{order.Value}/finalize";

        public string GetAccount(AccountId accountId) => $"{_baseUrl}/account/{accountId.Value}";
        

        public string GetAuthorization(OrderId orderId, AuthorizationId authorizationId) => $"{_baseUrl}/order/{orderId.Value}/authorization/{authorizationId.Value}";

        public string GetCertificate(OrderId order) => $"{_baseUrl}/order/{order.Value}/certificate";
        
        public string GetChallenge(OrderId orderId, AuthorizationId authorizationId, ChallengeId challengeId) => $"{_baseUrl}/order/{orderId.Value}/authorization/{authorizationId.Value}/challenge/{challengeId.Value}";

        public string GetOrder(OrderId order) => $"{_baseUrl}/order/{order.Value}";

        public string GetOrderList(AccountId accountId) => $"{_baseUrl}/account/{accountId.Value}/orders";

        public string KeyChange() => $"{_baseUrl}/key-change";

        public string NewAccount() => $"{_baseUrl}/new-account";

        public string NewNonce() => $"{_baseUrl}/new-nonce";

        public string NewOrder() => $"{_baseUrl}/new-order";

        public string ProfileMetadata(ProfileName profileName) => $"{_baseUrl}/profile/{profileName.Value}/metadata";       
        
        public string RevokeCert() => $"{_baseUrl}/revoke-cert";
    }
}
