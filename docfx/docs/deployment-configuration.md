# Configuring ACME-ADCS

You can either manually create a `appsettings.Production.json` or let the server help you in creating it's contents.  
Either way, the server comes with an appsettings-sample.json, which contains all possible settings with samples and default values.  

## Working files

ACME-ADCS needs a directory to store accounts, orders and issued certificates. Create one and set the ACL accordingly:

```pwsh
$acmeUser = "acme-user";
$acmeWorkingDir = "C:\ACME-ADCS";

New-Item $acmeWorkingDir -Type Directory;

$acl = Get-Acl $acmeWorkingDir
$aclRuleArgs = $acmeUser, "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
$acl.SetAccessRule($accessRule)
$acl | Set-Acl $acmeWorkingDir
```

## Manual configuration

Create `C:\inetpub\acme\appsettings.Production.json` in the editor of your choice.
Use the 'sample' file `appsettings-sample.json` as a template for your configuration file.
Make sure you configured at least one profile in the configuration file, which defines the `CAServer` and `TemplateName` to be used for issuing certificates.

Use `certutil` to get necessary information about your CA (or ask your CA-Admin):
```cmd
certutil -dump
```
Look for "Configuration" and set this as `CAServer` in the opened configuration file. Watch for '\', which need to be escaped. Refer to 'appsettings-sample.json' to see how it might look like.

Use `certutil -ADTemplate`, `certutil -CATemplates` or `certutil -Template` to find the name of the template to be used (or ask your CA-Admin) and set the `TemplateName` in the opened configuration file.

If you did not use `C:\ACME-ADCS` as your directory for working files, set it in the opened configuration file to the proper path.

A minimal configuration file supporting dns identifiers might look like this:
```json
{
  "AllowedHosts": "*",
  
  "AcmeServer": {
    "CanonicalHostname": "acme.th11s.corp",
    "CAAIdentities": [
      "acme.th11s.corp"
    ]
  },

  "AcmeFileStore": {
    "BasePath": "C:\\ACME-ADCS\\"
  },


  "Profiles": {
    "Default-DNS": {
      "SupportedIdentifiers": [ "dns" ],
      "CertificateServices": [
        {
          "CAServer": "adcs.th11s.corp\\cert-authority-1",
          "TemplateName": "acme-template"
        }
      ]
    }
  }
}
```

## Configuration creation tool

ACME-ADCS Server itself has a switch, that allows you to let it create a configuration for you. 

```cmd
cd C:\inetpub\acme\
ACMEServer.CLI.exe --config-tool
```

A wizzard will guide you through the configuration options.
When finished, you can automatically create an `appsettings.Production.json` file.


## Full annotated configuration sample 

You'll find this content as part of your deployed server in `appsettings-sample.json`

[!code-json[](../../src/ACMEServer.ADCS/appsettings-sample.json)]