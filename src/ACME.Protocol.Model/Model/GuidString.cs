using Microsoft.IdentityModel.Tokens;
using System;

namespace TGIT.ACME.Protocol.Model
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
