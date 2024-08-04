using Microsoft.IdentityModel.Tokens;

namespace Th11s.ACMEServer.Model
{
    public class GuidString
    {
        private GuidString()
        {
            Value = Base64UrlEncoder.Encode(Guid.NewGuid().ToByteArray());
        }

        private string Value { get; }

        public static string NewValue() => new GuidString().Value;
    }
}
