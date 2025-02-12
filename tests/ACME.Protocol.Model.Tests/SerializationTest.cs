using Newtonsoft.Json;
using Th11s.ACMEServer.Model;
using Xunit;

namespace ACME.Protocol.Model.Tests
{
    public class SerializationTest
    {
        [Fact]
        public void Account_Serialization_Roundtrips()
        {
            //TODO: Change test so it really tests a roundtrip.
            var jwkJson = @"{ ""kty"":""RSA"", ""n"": ""0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAtVT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn64tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FDW2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n91CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINHaQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw"", ""e"":""AQAB"", ""kid"":""2011-04-29""}";
            var jwk = new Jwk(jwkJson);
            var account = new Account(jwk, new[] { "some@example.com " }, null, null);

            var serialized = JsonConvert.SerializeObject(account);
            _ = JsonConvert.DeserializeObject<Account>(serialized);
        }
    }
}
