using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.HttpModel
{
    /// <summary>
    /// Represents the data of an ACME account
    /// https://tools.ietf.org/html/rfc8555#section-7.1.2
    /// </summary>
    public class Account
    {
        public Account(Model.Account model, string ordersUrl)
        {
            ArgumentNullException.ThrowIfNull(model);

            Status = EnumMappings.GetEnumString(model.Status);

            Contact = model.Contacts;
            TermsOfServiceAgreed = model.TOSAccepted.HasValue;

            ExternalAccountBinding = model.ExternalAccountBinding;
            Orders = ordersUrl;
        }

        public string Status { get; set; }
        public string? Orders { get; set; }

        public List<string>? Contact { get; set; }
        public bool? TermsOfServiceAgreed { get; set; }

        public AcmeJwsToken? ExternalAccountBinding { get; set; }
    }
}
