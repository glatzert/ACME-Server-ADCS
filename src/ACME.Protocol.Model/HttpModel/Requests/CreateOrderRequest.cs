using System;
using System.Collections.Generic;

namespace TGIT.ACME.Protocol.HttpModel.Requests
{
    public class CreateOrderRequest
    {
        public List<Identifier>? Identifiers { get; set; }

        public DateTimeOffset? NotBefore { get; set; }
        public DateTimeOffset? NotAfter { get; set; }
    }
}
