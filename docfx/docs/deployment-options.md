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
- _AspNetCore hosting bundle_ for the LTS version of .NET [10.0 LTS](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (recommended) or [8.0 LTS](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [ACME-Server-ADCS](https://github.com/glatzert/ACME-Server-ADCS/releases/latest)
