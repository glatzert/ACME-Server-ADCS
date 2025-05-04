using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Th11s.ACMEServer.Model.Configuration
{
    public class ProfileDescriptor
    {
        public string Name { get; set; } = "";

        public required string[] SupportedIdentifiers { get; set; } = [];

        public required ADCSOptions ADCSOptions { get; set; }
    }
}
