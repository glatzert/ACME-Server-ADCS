---
_layout: landing
---

# ACME-ADCS Server

This project enables you to use an ACME (RFC 8555) compliant client, to request certificates via Microsoft® Windows® Server Active Directory Certificate Services.  
The ACME (RFC 8555) protocol is famously used by Let's Encrypt® and thus there's a number of clients that can be used to obtain certificates.  
If you are into PowerShell, you can e.g. use my open source module [ACME-PS](https://www.powershellgallery.com/packages/ACME-PS/).

The server currently supports server certificate issuances and is able to handle http-01, dns-01 as well as tls-alpn-01 challenges.
For issuing client certificates it supports device-attest-01 challenges, which is currently in draft state and thus 'experimental'.
It needs an Microsoft ADCS for certificate issuance, that allows auto-enrollment for the template used with the server.

## Features

- ACME [(RFC 8555)](https://www.rfc-editor.org/rfc/rfc8555) compliant server for certificate issuance
- Certificate issuance via Microsoft® Windows® Server Active Directory Certificate Services
- Challenge types: `http-01`, `dns-01`, `tls-alpn-01`, `device-attest-01` (experimental, until standardized, Apple only currently, [more Information](./docs/device-attest.md)) 
- ExternalAccountBinding (EAB) support ([more Information](./docs/eab.md))
- Identifier types: `dns` ([RFC 8555](https://www.rfc-editor.org/rfc/rfc8555#section-9.7.7)), `ip` ([RFC 8738](https://www.rfc-editor.org/rfc/rfc8738)), `permanent-identifier` (experimental, [Draft](https://www.ietf.org/archive/id/draft-acme-device-attest-04.html))
- [Profiles](./docs/AboutProfiles.md) 'automatic' and ['client selected'](https://datatracker.ietf.org/doc/draft-aaron-acme-profiles/01/), which allow to define different settings for different identifiers, e.g. different templates or CA servers.

## License

Please be advised that this project is _NOT_ free for commercial-use, but you may test it in any company and use it for your personal projects as you see fit, please refer to the [LICENSE](https://github.com/glatzert/ACME-Server-ADCS/blob/main/LICENSE) file for details.
To obain a license for commercial-use, please [contact me via e-mail](mailto:thomas@th11s.de).  
Buying the license does not include maintenance, nevertheless we'll do our very best to answer issues here on GitHub as fast as possible.
If you need help installing the software or getting it up and running in your environment or you want a maintenance contract, use the email above and we'll figure something out.

The software is provided "as is", without warranty of any kind.

# Acknowledgements

This project uses software licensed under the MIT license:
- [BouncyCastle.Cryptography](https://github.com/bcgit/bc-csharp)
- [Coverlet](https://github.com/coverlet-coverage/coverlet)
- [DnsClient.Net](https://github.com/MichaCo/DnsClient.NET)
- [Karambolo.Extensions.Logging.File.Json](https://github.com/adams85/filelogger)
- [Microsoft.IdentitiyModel.Tokens](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet)
- [Microsoft AspNetCore](https://github.com/dotnet/aspnetcore)
- [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- [Webprofusion.Certify.ACME.Anvil](https://github.com/webprofusion/anvil)

And it uses software licensed under Apache 2.0 license
- [xunit](https://github.com/xunit/xunit)