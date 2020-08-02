# Install instructions

This small manual will show, how to install ACME-ACDS as IIS-WebSite.

Get a Windows Server 2019.  
It probably needs to be domain joined and should not have other purposes than issuing certificates via ACME.

Install IIS Server and required features:
```PowerShell
#Install required IIS Features
IIS PS> Install-WindowsFeature Web-Server,Web-Http-Logging,Web-Request-Monitor,Web-Http-Tracing,Web-Filtering,Web-IP-Security,Web-Mgmt-Console;
```

Copy the contents from the release to the wwwroot directory..

