# Quickstart Guid for ACME-ADCS

This small manual will show how to install ACME-ADCS as a website in IIS.

## ACME-ADCS Prerequisites

This guide assumes all machines are domain joined and that you have a working Active Directory Certificate Services (ADCS) instance.  
At least one ADCS template must be created, which allows auto-enrollment for the account used by the IIS-AppPool (see below).  


## Prepare IIS

- Install IIS (small list of helpers included, which make tracing easier).
```PowerShell
# Install required IIS Features
IIS PS> Install-WindowsFeature Web-Server,Web-Http-Logging,Web-Request-Monitor,Web-Http-Tracing,Web-Filtering,Web-IP-Security,Web-Mgmt-Console;
```

- Install the required [LTS Version of .NET](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (8.0 LTS). You'll need the hosting bundle from the .NET Runtime section.
- Download the latest release of [ACME-ADCS](https://github.com/glatzert/ACME-Server-ADCS/releases)

- Extract the contents of the Release ZIP-file into `C:\inetpub\wwwroot\`. (or another directory of your choice)
- Modify the IIS-AppPool, to not use .NET Framework (new .NET is loaded via another mechanism) and set its identity to either a group managed service account (recommended), custom account or "NetworkService".
- Add the account to the local [IIS_IUSRS](https://learn.microsoft.com/en-us/iis/get-started/planning-for-security/understanding-built-in-user-and-group-accounts-in-iis#understanding-the-new-iis_iusrs-group) group. Read more about [Application Pool Identities](https://learn.microsoft.com/en-us/iis/manage/configuring-security/application-pool-identities) and [Managed Service Accounts](https://learn.microsoft.com/de-de/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview)

 
## Configure ACME-ADCS

- Create a path for working files of ACME-ADCS, e.g `C:\ACME-ADCS` (this is the default path, any other will work)
- Grant read/write rights to the account used above

- Create `C:\inetpub\wwwroot\appsettings.Production.json` in the editor of your choice.
- Use the 'sample' file `appsettings-sample.json` as a template for your configuration file.
- Make sure you configured at least one profile in the configuration file, which defines the `CAServer` and `TemplateName` to be used for issuing certificates.

- Use `certutil` to get necessary information about your CA (or ask your CA-Admin):
```cmd
CMD> certutil -dump
```
- Look for "Configuration" and set this as `CAServer` in the opened configuration file. Watch for '\', which need to be escaped. Refer to 'appsettings-sample.json' to see how it might look like.

- Use `certutil -ADTemplate`, `certutil -CATemplates` or `certutil -Template` to find the name of the template to be used (or ask your CA-Admin)
- Set the `TemplateName` in the opened configuration file

- If you did not use `C:\ACME-ADCS` as your directory for working files, set it in the opened configuration file to the proper path.

A minimal configuration file supporting dns identifiers might look like this:
```json
{
  "AllowedHosts": "*",

  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    },

    "PathFormat": "C:\\ACME-ADCS\\Logs\\{Date}.json"
  },

  
  "AcmeServer": {
    "TOS": {
      "RequireAgreement": false
    }
  },

  "AcmeFileStore": {
    "BasePath": "C:\\ACME-ADCS\\"
  },


  "Profiles": {
    "Default-DNS": {
      "SupportedIdentifiers": [ "dns" ],
      "ADCSOptions": {
        "CAServer": "CA.FQDN.com\\CA Name",
        "TemplateName": "DNS-ACME-Template"
      }
    }
  }
}

```

## Finish

- Call `/` on your server in a browser and you should see the service-description file as required from ACME.
- Issue your first certificate with `certbot` or any other ACME compatible tool.

## Further Reading

ACME-ADCS supports a number of features, which are not covered in this quickstart guide. E.g.: device-attest-01 challenges, EAB support, ip identifiers, issuance profiles and more.
You can find more information in the docs folder of this repository or directly here:
- [About EAB](./docs/AboutEAB.md)
- [About Device Attest](./docs/AboutDeviceAttest.md)
- [About Profiles](./docs/AboutProfiles.md)

## Troubleshoot

- The server will default to write warnings and errors to the windows event log. Use this as starting point for troubleshooting.
- If you want to see more details, find the Logging:File section of the appsettings.json and use "Th11s":"Debug" as indicated by a comment in the file itself.
- Make sure your appsettings.Production.json is valid. You can use Powershell to validate the file:
```pwsh
#PWSH>
# Looks stupid, but removes all comments and trailing commas, which are not strictly valid JSON
$json = Get-Content .\appsettings.Production.json | ConvertFrom-Json | ConvertTo-Json -Depth 10
Test-Json -Json $json -SchemaFile .\appsettings-schema.json
```
