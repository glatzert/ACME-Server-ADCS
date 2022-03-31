# ACME-ADCS-Server

This projects enables you to use an ACME (RFC 8555) comliant client, to request certificates via Microsoft� Windows� Server Active Directory Certificate Services.  
The ACME (RFC 8555) protocol is famously used by Let's Encrypt� and thus there's a number of clients, that can be used to obtain certificates.  
If you are into PowerShell�, you can e.g. use my open source module [ACME-PS](https://www.powershellgallery.com/packages/ACME-PS/).

Please be adviced, that this project is _NOT_ free for commercial-use, but you may test it in any company and use it for your personally as you see fit.
Buying the license does not include maintenance, nevertheless I'll do my very best to answer to issues here in GitHub as fast as possible.
If you need help installing the software or get it up and running in your environment or you want a maintenance contract, feel free to contact me and we most likely will find a way.

The software is provided "as is", without warranty of any kind.

# Install instructions

This small manual will show, how to install ACME-ACDS as a website in ISS.
I assume your machine is domain joined.

## Prepare IIS

[] Install IIS (small list of helpers included, which make tracing easier).
```PowerShell
# Install required IIS Features
IIS PS> Install-WindowsFeature Web-Server,Web-Http-Logging,Web-Request-Monitor,Web-Http-Tracing,Web-Filtering,Web-IP-Security,Web-Mgmt-Console;
```

[] Install the [LTS Version of .NET](https://dotnet.microsoft.com/en-us/download). You'll need the hosting bundle from the .NET Runtime section.
[] Download the latest release of [ACME-ACDS](https://github.com/glatzert/ACME-Server-ACDS/releases)

[] Extract the contents of the Release ZIP-file into `C:\inetpub\wwwroot\`.
[] Modify the IIS-AppPool, to not use .NET Framework (new .NET is loaded via another mechanism) and set it's identity to either a custom account (recommended) or "NetworkService".

## Configure ACME-ACDS

[] Create a path for working files of ACME-ACDS, e.g `C:\ACME-ACDS` (this is the default path)
[] Grant full rights to the account used above

[] Copy `C:\inetpub\wwwroot\appsettings-custom.dist.json` to `C:\inetpub\wwwroot\appsettings-custom.json`
[] Open `C:\inetpub\wwwroot\appsettings-custom.json` in the editor of your choice.

[] Use `certutil` to get neccessary information about your CA (or ask your CA-Admin):
```cmd
CMD> certutil -dump
```
[] Look for "Configuration" and set this as `CAServer` in `appsettings.json`

[] Use `certutil -ADTemplate`, `certutil -CATemplates` or `certutil -Template` to find the name of the template to be used (or ask your CA-Admin)
[] Set the `TemplateName` in `appsettings.json`

If you did not use `C:\ACME-ACDS` as your directory for working files, set it in `appsettings.json` to the proper path.

## Finish

[] Call `/` on your server in a browser and you should see the service-description file as required from ACME.
[] Issue your first certificate with `certbot` or any other ACME compatible tool.

## Troubleshoot

[] The server will default to write warnings and errors to the windows event log. Use this as starting point for troubleshooting.