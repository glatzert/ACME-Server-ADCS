# ACME-ADCS-Server

This project enables you to use an ACME (RFC 8555) compliant client, to request certificates via Microsoft® Windows® Server Active Directory Certificate Services.  
The ACME (RFC 8555) protocol is famously used by Let's Encrypt® and thus there's a number of clients that can be used to obtain certificates.  
If you are into PowerShell, you can e.g. use my open source module [ACME-PS](https://www.powershellgallery.com/packages/ACME-PS/).

The server currenttly supports server certificates only and is able to handle http-01, dns-01 as well as tls-alpn-01 challenges.
It needs an Microsoft ADCS for certificate issuance, that allows auto-enrollment for the template used with the server.

## License

Please be advised that this project is _NOT_ free for commercial-use, but you may test it in any company and use it for your personal projects as you see fit.
Buying the license does not include maintenance, nevertheless I'll do my very best to answer issues here on GitHub as fast as possible.
If you need help installing the software or getting it up and running in your environment or you want a maintenance contract, feel free to [contact me via e-Mail](mailto:th11s@outlook.de) and we'll figure something out.

The software is provided "as is", without warranty of any kind.

## Implemented features

- ACME compliant server for certificate issuance
- Certificate issuance via Microsoft® Windows® Server Active Directory Certificate Services
- Challenge types: `http-01`, `dns-01`, `tls-alpn-01`
- ExternalAccountBinding (EAB) support


# Install instructions

This small manual will show how to install ACME-ADCS as a website in IIS.
I assume your machine is domain joined.

## ACME-ADCS Prerequisites

- [ ] You need a working Active Directory Certificate Services (ADCS) instance.
- [ ] Create a certificate template, that enables auto-enrollment for the account used by the IIS-AppPool (see below).
- [ ] Allow certificate issuance based on the certificate template in your ADCS instance.

## Prepare IIS

- [ ] Install IIS (small list of helpers included, which make tracing easier).
```PowerShell
# Install required IIS Features
IIS PS> Install-WindowsFeature Web-Server,Web-Http-Logging,Web-Request-Monitor,Web-Http-Tracing,Web-Filtering,Web-IP-Security,Web-Mgmt-Console;
```

- [ ] Install the required [LTS Version of .NET](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (8.0 LTS). You'll need the hosting bundle from the .NET Runtime section.
- [ ] Download the latest release of [ACME-ADCS](https://github.com/glatzert/ACME-Server-ADCS/releases)

- [ ] Extract the contents of the Release ZIP-file into `C:\inetpub\wwwroot\`.
- [ ] Modify the IIS-AppPool, to not use .NET Framework (new .NET is loaded via another mechanism) and set its identity to either a group managed service account (recommended), custom account or "NetworkService".
- [ ] Add the account to the local [IIS_IUSRS](https://learn.microsoft.com/en-us/iis/get-started/planning-for-security/understanding-built-in-user-and-group-accounts-in-iis#understanding-the-new-iis_iusrs-group) group. Read more about [Application Pool Identities](https://learn.microsoft.com/en-us/iis/manage/configuring-security/application-pool-identities) and [Managed Service Accounts](https://learn.microsoft.com/de-de/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview)

## Configure ACME-ADCS

- [ ] Create a path for working files of ACME-ADCS, e.g `C:\ACME-ADCS` (this is the default path)
- [ ] Grant read/write rights to the account used above

- [ ] Copy `C:\inetpub\wwwroot\appsettings-custom.dist.json` to `C:\inetpub\wwwroot\appsettings-custom.json`
- [ ] Open `C:\inetpub\wwwroot\appsettings-custom.json` in the editor of your choice.

- [ ] Use `certutil` to get necessary information about your CA (or ask your CA-Admin):
```cmd
CMD> certutil -dump
```
- [ ] Look for "Configuration" and set this as `CAServer` in the opened configuration file. Watch for '\', which need to be escaped. Refer to 'appsettings-sample.json' to see how it might look like.

- [ ] Use `certutil -ADTemplate`, `certutil -CATemplates` or `certutil -Template` to find the name of the template to be used (or ask your CA-Admin)
- [ ] Set the `TemplateName` in the opened configuration file

- [ ] If you did not use `C:\ACME-ADCS` as your directory for working files, set it in the opened configuration file to the proper path.

## Finish

- [ ] Call `/` on your server in a browser and you should see the service-description file as required from ACME.
- [ ] Issue your first certificate with `certbot` or any other ACME compatible tool.

## Troubleshoot

- [ ] The server will default to write warnings and errors to the windows event log. Use this as starting point for troubleshooting.
