# ACME-ADCS-Server

This projects enables you to use an ACME (RFC 8555) comliant client, to request certificates via Microsoft® Windows® Server Active Directory Certificate Services.  
The ACME (RFC 8555) protocol is famously used by Let's Encrypt® and thus there's a number of clients, that can be used to obtain certificates.  
If you are into PowerShell®, you can e.g. use my open source module [ACME-PS](https://www.powershellgallery.com/packages/ACME-PS/).

Please be adviced, that this project uses the License-Zero Prosperity license and thus is _NOT_ free for commercial-use.  
Buying the license does not include maintenance, nevertheless I'll do my very best to answer to issues here in GitHub as fast as possible.  
If you need help installing the software or get it up and running in your environment, feel free to contact me and we most likely will find a way.

# Install instructions

This small manual will show, how to install ACME-ACDS as a website in ISS or a windows-service.  

Get a Windows Server 2019.  
It probably needs to be domain joined and should not have other purposes than issuing certificates via ACME.

## As IIS application

Install IIS Server and required features:
```PowerShell
# Install required IIS Features
IIS PS> Install-WindowsFeature Web-Server,Web-Http-Logging,Web-Request-Monitor,Web-Http-Tracing,Web-Filtering,Web-IP-Security,Web-Mgmt-Console;
```

- Copy the contents from the release to the wwwroot directory.
- Modify the application pool to not use .NET and set it's executing identity to either an own account or to "NetworkService" ("LocalSystem" will also do well).

## A

- Create the path C:\ACME-ACDS and make it read- and writeable for the aforementioned account.
- Create a new appsettings.json and fill `CAServer` and `TemplateName` in `ACDSIssuer`.
