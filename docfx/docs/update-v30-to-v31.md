# V3.0 to V3.1

The configuration is backwards-compatible, but has deprecated `ACMEOptions` from the Profiles. Update your `appsettings.Production.json` to use `CertificateServices`

From the minimal sample below
```diff
  "Profiles": {
    "Default-DNS": {
      "SupportedIdentifiers": [ "dns" ],
-      "ADCSOptions": {
-        "CAServer": "CA.FQDN.com\\CA Name",
-        "TemplateName": "DNS-ACME-Template"
-      }
+      "CertificateServices": [
+        {
+          "CAServer": "CA.FQDN.com\\CA Name",
+          "TemplateName": "DNS-ACME-Template"
+        }
+      ]
    }
  }
```