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
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Th11s.ACMEServer.CLI;

namespace Th11s.ACMEServer.CLI;

public class ADCertificationAuthority
{
    /// <summary>
    ///     Builds the object from a SearchResult containing a pKIEnrollmentService LDAP object.
    /// </summary>
    /// <param name="searchResult">The Active Directory SearchResult to build the object from.</param>
    public ADCertificationAuthority(SearchResult searchResult)
    {
        const string enrollPermission = "0E10C968-78FB-11D2-90D4-00C04F79DC55";

        Name = (string)searchResult.Properties["cn"][0];

        ConfigurationString = $"{searchResult.Properties["dNSHostName"][0]}\\{Name}";

        //Certificate = GetCertificate((byte[])searchResult.Properties["cACertificate"][0], textualEncoding);

        CertificateTemplates =
            (from object certificateTemplate in searchResult.Properties["certificateTemplates"]
             select certificateTemplate.ToString()).ToList();

        CertificateTemplates.Sort();

        var rawSecurityDescriptor =
            new RawSecurityDescriptor((byte[])searchResult.Properties["ntSecurityDescriptor"][0], 0);

        if (rawSecurityDescriptor == null || rawSecurityDescriptor.DiscretionaryAcl == null)
        {
            return;
        }

        foreach (var genericAce in rawSecurityDescriptor.DiscretionaryAcl)
        {
            if (genericAce is not ObjectAce objectAce)
            {
                continue;
            }

            if (objectAce.ObjectAceType != new Guid(enrollPermission))
            {
                continue;
            }

            switch (objectAce.AceType)
            {
                case AceType.AccessAllowedObject:
                    AllowedPrincipals.Add(objectAce.SecurityIdentifier);
                    break;
                case AceType.AccessDeniedObject:
                    DisallowedPrincipals.Add(objectAce.SecurityIdentifier);
                    break;
            }
        }
    }

    /// <summary>
    ///     The common name of the certification authority.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The configuration string for this certification authority.
    /// </summary>
    [JsonIgnore]
    public string ConfigurationString { get; }

    /// <summary>
    ///     A list of all certificate templates offered by the certification authority.
    /// </summary>
    public List<string> CertificateTemplates { get; }

    /// <summary>
    ///     The current certification authority certificate of the certification authority.
    /// </summary>
    public string? Certificate { get; }

    private List<SecurityIdentifier> AllowedPrincipals { get; } = new();
    private List<SecurityIdentifier> DisallowedPrincipals { get; } = new();


    /// <summary>
    ///     Determines whether a given WindowsIdentity may enroll for certificates from this certification authority.
    /// </summary>
    /// <param name="identity">The Windows identity to check for permissions.</param>
    /// <param name="explicitlyPermitted">Return true only if the identity is explicitly mentioned in the acl.</param>
    /// <returns></returns>
    public bool AllowsForEnrollment(WindowsIdentity identity, bool explicitlyPermitted = false)
    {
        if(identity.User == null)
        {
            return false;
        }

        var isAllowed = false;
        var isDenied = false;

        if (!explicitlyPermitted)
        {
            for (var index = 0; index < identity.Groups?.Count; index++)
            {
                var group = (SecurityIdentifier)identity.Groups[index];
                isAllowed = AllowedPrincipals.Contains(group) || isAllowed;
                isDenied = DisallowedPrincipals.Contains(group) || isDenied;
            }
        }

        isAllowed = AllowedPrincipals.Contains(identity.User) || isAllowed;
        isDenied = DisallowedPrincipals.Contains(identity.User) || isDenied;

        return isAllowed && !isDenied;
    }

}
