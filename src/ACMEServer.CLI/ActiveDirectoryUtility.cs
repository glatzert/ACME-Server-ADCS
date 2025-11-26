// Copyright (c) Uwe Gradenegger <info@gradenegger.eu>
// This is a derived work by Thomas Ottenhus <thomas@th11s.de>

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


// Copyright (c) Uwe Gradenegger <info@gradenegger.eu>
// This is a derived work by Thomas Ottenhus <thomas@th11s.de>

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.DirectoryServices;
using Th11s.ACMEServer.CLI;

namespace Th11s.ACMEServer.CLI;

internal static class ActiveDirectoryUtility
{
    /// <summary>
    ///     Returns a SearchResultCollection holding pKIEnrollmentService objects found in the directory.
    /// </summary>
    public static List<ADCertificationAuthority> GetEnrollmentServiceCollection()
    {
        using var rootDSE = new DirectoryEntry("LDAP://RootDSE");
        var forestRootDomain = (string)rootDSE.Properties["rootDomainNamingContext"].Value!;

        var enrollmentContainerDN =
            $"LDAP://CN=Enrollment Services,CN=Public Key Services,CN=Services,CN=Configuration,{forestRootDomain}";

        using var searchRootEntry = new DirectoryEntry(enrollmentContainerDN);
        using var directorySearcher = new DirectorySearcher(searchRootEntry)
        {
            Filter = $"(objectCategory=pKIEnrollmentService)",
            Sort = new SortOption("cn", SortDirection.Ascending),
            PropertiesToLoad =
            { "cn", "certificateTemplates", "dNSHostName", "cACertificate", "ntSecurityDescriptor" },
            SecurityMasks = SecurityMasks.Dacl
        };

        return [.. directorySearcher.FindAll()
            .OfType<SearchResult>()
            .Select(x => new ADCertificationAuthority(x))
        ];
    }
}
