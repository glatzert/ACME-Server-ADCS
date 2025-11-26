## Installation

Download the latest release of [ACME-ADCS](https://github.com/glatzert/ACME-Server-ADCS/releases) with the matching .NET runtime version and extract it into the folder you prepared.

Create a path for working files of ACME-ADCS, e.g `C:\ACME-ADCS` (this is the default path, any other will work)
Grant read/write rights to the account used above

## Configuration

You can either manually create a `appsettings.Production.json` or let the server help you in creating it's contents.

### Configuration creation tool

ACME-ADCS Server itself has a switch, that allows you to let it create a configuration for you. 
```
CMD> C:\ACME-Server\ACMEServer.ACDS.exe --config-tool
```
A wizzard will guide you through the configuration options.
When finished, you can automatically create an `appsettings.Production.json` file.

### Manual configuration

Create `C:\inetpub\acme\appsettings.Production.json` in the editor of your choice.
Use the 'sample' file `appsettings-sample.json` as a template for your configuration file.
Make sure you configured at least one profile in the configuration file, which defines the `CAServer` and `TemplateName` to be used for issuing certificates.

Use `certutil` to get necessary information about your CA (or ask your CA-Admin):
```cmd
CMD> certutil -dump
```
Look for "Configuration" and set this as `CAServer` in the opened configuration file. Watch for '\', which need to be escaped. Refer to 'appsettings-sample.json' to see how it might look like.

Use `certutil -ADTemplate`, `certutil -CATemplates` or `certutil -Template` to find the name of the template to be used (or ask your CA-Admin) and set the `TemplateName` in the opened configuration file.

If you did not use `C:\ACME-ADCS` as your directory for working files, set it in the opened configuration file to the proper path.

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
    "CAAIdentities": [
      "FQDN.com"
    ]
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
