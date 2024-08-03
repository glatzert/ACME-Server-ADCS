using System.Collections.Generic;

namespace TGIT.ACME.Protocol.HttpModel.Requests
{
    public class UpdateAccount
    {
        public List<string>? Contact { get; set; }
        public string? Status { get; set; }
    }
}
