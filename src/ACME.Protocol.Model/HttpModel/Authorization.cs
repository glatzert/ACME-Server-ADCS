using System.Collections.Generic;
using System.Globalization;

namespace TGIT.ACME.Protocol.HttpModel
{
    /// <summary>
    /// Represents an ACME authorization object
    /// https://tools.ietf.org/html/rfc8555#section-7.1.4
    /// </summary>
    public class Authorization
    {
        public Authorization(Model.Authorization model, IEnumerable<Challenge> challenges)
        {
            if (model is null)
                throw new System.ArgumentNullException(nameof(model));

            if (challenges is null)
                throw new System.ArgumentNullException(nameof(challenges));

            Status = EnumMappings.GetEnumString(model.Status);

            Expires = model.Expires.ToString("o", CultureInfo.InvariantCulture);
            Wildcard = model.IsWildcard;

            Identifier = new Identifier(model.Identifier);
            Challenges = new List<Challenge>(challenges);
        }

        public string Status { get; }

        public Identifier Identifier { get; }
        public string? Expires { get; }
        public bool? Wildcard { get; }

        public IEnumerable<Challenge> Challenges { get; }
    }
}
