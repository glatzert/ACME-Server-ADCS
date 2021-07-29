using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TGIT.ACME.Protocol.HttpModel
{
    /// <summary>
    /// Represents an ACME order
    /// https://tools.ietf.org/html/rfc8555#section-7.1.3
    /// </summary>
    public class Order
    {
        public Order(Model.Order model,
            IEnumerable<string> authorizationUrls, string finalizeUrl, string certificateUrl)
        {
            if (model is null)
                throw new System.ArgumentNullException(nameof(model));

            if (authorizationUrls is null)
                throw new System.ArgumentNullException(nameof(authorizationUrls));

            if (string.IsNullOrEmpty(finalizeUrl))
                throw new System.ArgumentNullException(nameof(finalizeUrl));

            if (string.IsNullOrEmpty(certificateUrl))
                throw new System.ArgumentNullException(nameof(certificateUrl));

            Status = EnumMappings.GetEnumString(model.Status);
            
            Expires = model.Expires?.ToString("o", CultureInfo.InvariantCulture);
            NotBefore = model.NotBefore?.ToString("o", CultureInfo.InvariantCulture);
            NotAfter = model.NotAfter?.ToString("o", CultureInfo.InvariantCulture);

            Identifiers = model.Identifiers.Select(x => new Identifier(x)).ToList();

            Authorizations = new List<string>(authorizationUrls);

            if(model.Status == Model.OrderStatus.Ready)
                Finalize = finalizeUrl;

            if(model.Status == Model.OrderStatus.Valid)
                Certificate = certificateUrl;

            if(model.Error != null)
                Error = new AcmeError(model.Error);
        }

        public string Status { get; }

        public List<Identifier> Identifiers { get; }

        public string? Expires { get; }
        public string? NotBefore { get; }
        public string? NotAfter { get; }

        public AcmeError? Error { get; }

        public List<string> Authorizations { get; }
        
        public string? Finalize { get; }
        public string? Certificate { get; }
    }
}
