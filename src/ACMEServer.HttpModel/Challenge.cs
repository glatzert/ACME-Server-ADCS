using System.Globalization;
using Th11s.ACMEServer.Model.Exceptions;

namespace Th11s.ACMEServer.HttpModel
{
    /// <summary>
    /// Represents an ACME challenge
    /// https://tools.ietf.org/html/rfc8555#section-7.1.5
    /// </summary>
    public class Challenge
    {
        public Challenge(Model.Challenge model, string challengeUrl)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrEmpty(challengeUrl))
                throw new ArgumentNullException(nameof(challengeUrl));

            Type = model.Type;
            Token = model.Token;

            Status = EnumMappings.GetEnumString(model.Status);
            Url = challengeUrl;

            Validated = model.Validated?.ToString("o", CultureInfo.InvariantCulture);
            Error = model.Error != null ? new AcmeError(model.Error) : null;
        }


        public string Type { get; }
        public string Token { get; }

        public string Status { get; }

        public string? Validated { get; }
        public AcmeError? Error { get; }

        public string Url { get; }
    }
}
