# ACME-ADCS-Server

Diese Software ermöglicht ihnen, einen ACME (RFC 8555) konformen Client zu verwenden, 
um Zertifikate über Microsoft® Windows® Server Active Directory Certificate Services (ADCS) anzufordern.
Das ACME-Protokoll wurde bekannt durch die Einführung von [Let's Encrypt](https://letsencrypt.org/) und 
es existieren [diverse Clients](https://letsencrypt.org/docs/client-options/), die verwendet werden können, um Zertifikate auszustellen.

## System-Voraussetzungen

Zum Betrieb der Software werden zwei Windows Server 2022 (oder neuer) benötigt:
1. **ADCS-Server**: Dieser Server stellt die Zertifikate aus.
1. **Web-Server**: Dieser Server stellt die ACME-Endpunkte bereit.

Des weiteren benötigt der Web-Server die aktuelle .NET LTS Version und die Software selbst.

### Web-Server

Der Web-Server sollte wie folgt Konfiguriert sein:
1. **Server-Konfiguration**: Der Server muss Mitglied einer Domäne sein.
1. **IIS**: Internet-Information-Services (IIS) muss installiert sein und die folgenden Features aktiviert haben:
	- Web-Server
1. **TLS**: Der Web-Server sollte über ein gültiges TLS-Zertifikat verfügen.
1. **Asp.NetCore**: Die [LTS Version von .NET](https://dotnet.microsoft.com/en-us/download/dotnet/) Hosting-Bundle muss installiert sein.
1. **AppPool**: Als ausführender Benutzer sollte ein Gruppen-Managed-Service-Account (gMSA) verwendet werden - dieser Benutzer muss das Auto-Enroll Recht für das Zertifikat-Template dem ADCS-Server haben.
1. **Zugriffsrechte**: Der ausführende Benutzer muss Mitglied der lokalen Gruppe `IIS_IUSRS` sein.


### ADCS-Server

Der ADCS-Server muss wie folgt konfiguriert sein:
1. **Server-Konfiguration**: Der Server muss Mitglied einer Domäne sein und ADCS im Enterprise-Mode installiert haben.
1. **Zertifikat-Templates**: Es muss ein Zertifikat-Template existieren, welches die automatische Ausstellung von Zertifikaten für den ACME-Server Benutzer erlaubt.


## Installation

Die Installationsanleitung befasst sich ausschließlich mit der Installation des ACME-Servers auf dem Web-Server.

1. **Vorbereitung**: Stellen Sie sicher, dass alle Voraussetzungen erfüllt sind.
1. **Download**: Laden Sie die aktuelle Version des ACME-Servers herunter.
1. **Extrahieren**: Entpacken Sie das Archiv in das Verzeichnis `C:\inetpub\wwwroot\`.
1. **Konfiguration**: Passen Sie die Konfiguration in der Datei `appsettings-custom.json` an.
	- Verwenden sie `certutil -dump` um die CA-Server-Konfiguration zu ermitteln.
	- Verwenden sie `certutil -ADTemplate`, `certutil -CATemplates` oder `certutil -Template` um den Namen des Zertifikat-Templates zu ermitteln.
1. **Zugriffsrechte**: Stellen Sie sicher, dass der ausführende Benutzer Schreibrechte auf das Arbeitsverzeichnis hat.
1. **Starten**: Rufen Sie die URL `https://<server>/` auf und prüfen Sie, ob der ACME-Server antwortet.

1. 
## Troubleshooting

Die Log-Dateien des ACME-Servers finden Sie im Arbeitsverzeichnis (`C:\ACME-ADCS\`).  
Oft erkennen Sie hier bereits Probleme mit lokalen Zugriffsrechten oder Probleme beim Ausstellen der Zertifikate.
Fehler der Zertifikatsausstellung werden in der Windows Ereignisanzeige (des ADCS) protokolliert und sind ggf. auch im CA-Snapin des ADCS-Servers sichtbar.
