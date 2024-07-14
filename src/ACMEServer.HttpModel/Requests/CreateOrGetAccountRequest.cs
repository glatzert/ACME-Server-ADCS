using System.Collections.Generic;

namespace TGIT.ACME.Protocol.HttpModel.Requests
{
    public class CreateOrGetAccount
    {
        public List<string>? Contact { get; set; }

        public bool TermsOfServiceAgreed { get; set; }
        public bool OnlyReturnExisting { get; set; }
    }
}
