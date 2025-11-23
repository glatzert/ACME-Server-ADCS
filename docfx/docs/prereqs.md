# Prerequisits

To get everything up and running, you'll need essentially two servers:
- a server with the role Microsoft Active Directory Certificate Services (ADCS)
- a server that will act as ACME server and will get the software installed. I recommend hosting the software on IIS (standalone or for mutliple applications), but running as a service will also be described.

Both servers should be part of a Windows Domain.

## Process identity

Create an service account in your AD, that will be used to run the ACME process.  
I highly recommend using gMSA or similar types of accounts, so passwords are not a concern.  
If you use IIS as recommended, add that account to the local [IIS_IUSRS](https://learn.microsoft.com/en-us/iis/get-started/planning-for-security/understanding-built-in-user-and-group-accounts-in-iis#understanding-the-new-iis_iusrs-group) group. (Read more about [Application Pool Identities](https://learn.microsoft.com/en-us/iis/manage/configuring-security/application-pool-identities) and [Managed Service Accounts](https://learn.microsoft.com/de-de/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview))

## ADCS preparations

On the ADCS server, create one or more certificate template(s), that will be used by the ACME server.
The certificate templates need to have **auto-enrollment** enabled for the ACME process identity.  
Also make sure, the templates allow RSA and ECDSA certificates to be issued.

If you want to allow the ACME server to revoke certificates via the ACME protocol, also make sure to allow the process identity to revoke certificates.

## IIS Preparation

ACME-ADCS does use IIS only as a 'reverse-proxy' of sorts, so not much is to be configured here.
You can use this line of PowerShell to install IIS and some helpful modules:

```pwsh
IIS PS> Install-WindowsFeature Web-Server,Web-Http-Logging,Web-Request-Monitor,Web-Http-Tracing,Web-Filtering,Web-IP-Security,Web-Mgmt-Console;
```

Install the required AspNetCore hosting bundle LTS Version of .NET [8.0 LTS](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or [10.0 LTS](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

Create an application pool, that uses 'no managed code' (AspNetCore does not use IIS built-in framework anymore) and uses the process identity you just created.  
Prepare a directory (e.g. `C:\inetpub\acme`) for the ACME server and create an IIS site pointing to that location (or use 'Default Web Site' and `C:\inetpub\wwwroot`). Assign the application pool you created.

