# V3.0 to V3.1

There are two changes, that might require manual intervention:

## Deprecation of ADCSOptions

The configuration is backwards-compatible, but has deprecated `ADCSOptions` from the Profiles. Update your `appsettings.Production.json` to use `CertificateServices`

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

## Profile Priority

Before V3.1, if you had multiple issuance profiles matching and the client did not specify a specific profile, the server would reliably pick the same profile as a 'default'.  
This had not been codified before, so it was more an side effect of an implementation detail. That detail changed, so it can now happen, that profiles are picked randomly.

To prevent that, a profile now can specify it's priority. Profiles with higher priority are picked first.

```diff
  "Profiles": {
    "Default-DNS": {
      "SupportedIdentifiers": [ "dns" ],
+      "Priority": 10
    },
    "More-Default-DNS": {
      "SupportedIdentifiers": [ "dns" ],
+      "Priority": 100
    }
  }
```