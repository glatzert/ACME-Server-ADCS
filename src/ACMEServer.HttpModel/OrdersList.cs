using System.Collections.Generic;
using System.Linq;

namespace TGIT.ACME.Protocol.HttpModel
{
    /// <summary>
    /// Represents a list of order urls
    /// </summary>
    public class OrdersList
    {
        public OrdersList(IEnumerable<string> orders)
        {
            Orders = orders.ToList();
        }

        public List<string> Orders { get; set; }
    }
}
