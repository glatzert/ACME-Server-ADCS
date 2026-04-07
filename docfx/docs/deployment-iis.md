# Deploy to IIS

ACME-ADCS does use IIS only as a 'reverse-proxy' of sorts, so not much is to be configured here.
You can use this line of PowerShell to install IIS and it's management console:

```pwsh
IIS PS> Install-WindowsFeature Web-Server,Web-Mgmt-Console;
```

Also execute the installer of the downloaded .NET hosting bundle.

Create an application pool that uses 'no managed code' and use `acme-user` as the process identity.  
Prepare a directory (e.g. `C:\inetpub\acme`) for ACME-ADCS's files and create an IIS site pointing to that location (alternatively use 'Default Web Site' and `C:\inetpub\wwwroot`).
Assign the created application pool.

Make sure `acme-user` is member of the local group [IIS_IUSRS](https://learn.microsoft.com/en-us/iis/get-started/planning-for-security/understanding-built-in-user-and-group-accounts-in-iis#understanding-the-new-iis_iusrs-group).

Extract the downloaded ACME-ADCS files to `C:\inetpub\acme`.
