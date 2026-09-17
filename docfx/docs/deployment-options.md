# Deployment options

To get everything up and running you'll essentially need two server roles:
- a server with the role **Microsoft Active Directory Certificate Services (ADCS)**
- a server that will act as **ACME server** and will get the software deployed to.

Both servers should be joined to the same Microsoft Windows Domain as the ADCS.

ACME-ADCS can be run on IIS or be started as a service (using Kestrel the AspNetCore Built-In web server).
Colocation of ACME-ADCS on the ADCS server is possible. 

The documentation will use the server names `acme.th11s.corp` and `adcs.th11s.corp\cert-authority-1` in samples.

## Downloads

Regardless of the choosen deployment option you need to have the following pieces of software downloaded:
- _AspNetCore hosting bundle_ for the LTS version of .NET [10.0 LTS](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [ACME-Server-ADCS](https://github.com/glatzert/ACME-Server-ADCS/releases/latest)

## Experimental Tooling

ACME-ADCS comes with two experimental tools, that are - as of now - only superficially tested:
- `ACMEServer.CLI.exe --config-tool` to create a configuration for the server
- `Deploy-ACMEServer.ADCS.ps1` to download, deploy and configure the server. [Script on GitHub](https://github.com/glatzert/ACME-Server-ADCS/blob/main/Deploy-ACMEServer.ADCS.ps1)