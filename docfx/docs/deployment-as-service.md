# Deploy as Windows service

Execute the installer of the downloaded .NET hosting bundle.

Create a directory to extract the downloaded ACME-ADCS version (e.g. `C:\ACME-ADCS-Server`).  
Then create the windows service [as described here](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0&tabs=visual-studio#create-and-manage-the-windows-service):

## Configure Windows service

```pwsh
$acmeDir = "C:\ACME-ADCS-Server"
$acmeUser = "acme-user";

$acl = Get-Acl $acmeDir
$aclRuleArgs = $acmeUser, "Read,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
$acl.SetAccessRule($accessRule)
$acl | Set-Acl $acmeDir

$acmeExecutable = Join-Path $acmeDir "ACMEServer.ADCS.exe"
New-Service -Name "ACME-ADCS" -BinaryPathName "$acmeExecutable --contentRoot $acmeDir" -Credential "acme-user" -Description "ACME-ADCS Server" -DisplayName "ACME-ADCS Server" -StartupType Automatic
```

Make sure the `acme-user` has [logon as service rights](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0&tabs=visual-studio#log-on-as-a-service-rights).

Also check the service properties, if you want to enable restart after failure.

## Configure Kestrel

As of V3.1 you can place Kestrels (that is the built-in http server) configuration in a file called. `appsettings.Kestrel.json`. Before V3.1 you need to put it into `appsettings.Production.json`.

The config file is simple but essential, since it creates the endpoints that the server will listen to.
Here's a simple example on how you might do it, when there's a certificate in the store.

```json
{
  "Kestrel": {
    "Endpoints": {
      "HttpsInlineCertStore": {
        "Url": "https://acme.th11s.corp",
        "Certificate": {
          "Subject": "acme.th11s.corp",
          "Store": "My",
          "Location": "localMachine",
          "AllowInvalid": "false"
        }
      }
    }
  }
}
```

Microsoft provides all the details about having the endpoints in an [appsettings.json file](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0#configure-endpoints-in-appsettingsjson) and how to [enable https](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0#configure-https-in-appsettingsjson).