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

The settings en detail:
**AcmeFileStore:BasePath**   
This is the root working directory for ACME-ADCS, it will be used to store accounts, orders, issued certificates and nonces. Once you issued a certificate, feel free to browse the contents - it's all cleartext json files.

**AcmeServer:CanonicalHostname**  
This is needed to create canonical account uris, which are used by CAA and dns-persist-01. All other uris will also use this hostname when they are created. Make sure the server is reachable via http on this hostname.

**AcmeServer:CAAIdentities**  
The server needs to know which [CAA entries](https://en.wikipedia.org/wiki/DNS_Certification_Authority_Authorization) are meant for it. Provide those names here. The identities will also be used in dns-persist-01, as it uses the same format as CAA.  
If you did not provide a canonical hostname, the first CAA entry will be used to create uris, where neccessary.

**Profiles**  
You can define as many profiles as you like and the names are arbitrary (`Default-DNS` was used as a sample here).  
**SupportedIdentifiers** restricts the profile to dns identifiers, meaning neither ip, email or permanent-identifiers would be usable with this profile.  
**CertificateServices** defines which **CAServer** and **TemplateName** will be used to create the certificates.
Certificate services is an array, since you might want to define multiple CA / template combinations depending on the key-algorithm and key-size.


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