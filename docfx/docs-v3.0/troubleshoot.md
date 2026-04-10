## Finish

- Call `/` on your server in a browser and you should see the service-description file as required from ACME.
- Issue your first certificate with `certbot` or any other ACME compatible tool.

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