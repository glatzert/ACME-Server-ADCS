# Trouble?

Where to look when the server does not start or the certificates cannot be issued?
The following questions might help to pin point the problem:

- Does the server run at all?
- Do different clients fail or a single one?
- At which step does the server fail?

## Log files / Event logs

There are four main log locations for the services involved:
- Windows event log of the ACME-ADCS machine
- log files written by ACME-ADCS itself (`C:\ACME-ADCS\logs`)
- Windows event log of the ADCS machine
- 'Failed Requests' list of the certificate authority

## Does it run at all?

Calling `https://acme.th11s.corp` in your browser should show you the service-description as required by ACME protocol. If that's not the case go to Windows event viewers _Application_ log and look for **.NET Runtime** and **IIS AspNetCore Module** events. It should tell you why it could not start.

It that's not conclusive you can try to run the server software from a cmd line (`C:\inetpub\acme\ACMEServer.ADCS.exe`). It might tell you something is off with your _appsettings.*.json_.

Make sure your appsettings.Production.json is valid. You can use Powershell to validate the file:
```pwsh
#PWSH>
# Looks stupid, but removes all comments and trailing commas, which are not strictly valid JSON
$json = Get-Content .\appsettings.Production.json | ConvertFrom-Json | ConvertTo-Json -Depth 10
Test-Json -Json $json -SchemaFile .\appsettings-schema.json
```

If this does not help, you can modify the _web.config_ to output everything that would normally go to console.

## Do different clients fail?

There are well established clients like certbot, Posh-ACME, simple-acme, acme.sh, and similar. If those don't work correctly there might be something defect, but there are less common clients like HPE ILO7 - where some specific case might occure. So if possible use a well established client to check, if the service is okay.

## At which step does the server fail?

ACME-ADCS does write rather extensive logs to `C:\ACME-ADCS\logs`. If that is not conclusive you can even enable debug logging in `appsettings.json` (it's commented how to do it there). The debug logs will allow a rather detailed look into what's happening to a specific request. You might even enable http logging to trace http requests and responses.