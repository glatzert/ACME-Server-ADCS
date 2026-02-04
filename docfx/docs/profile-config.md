# Profiles (or issuance profiles)

Profiles are used to define rules and settings for specific types of certificate requests and orders in ACME-ADCS via `appsettings.Production.json`.
There are essentially two ways to select a profile: 

1. via the `profile` query parameter in the ACME client request
1. by the ACME-ADCS server, based on the identifiers used in the certificate order

A profile contains the supported identifier types, validation rules and the settings for issuing certificates.
The following profile would allow issuing DNS and IP certificates for any account, without any special restrictions:

```json
  "Profiles": {
    // A sample for a DNS and IP profile, the name 'Default' is arbitrary, you can choose any name you like.
    "Default": {
      "SupportedIdentifiers": [ "dns", "ip" ],

      // Optionally you can set allowed challenge-types:
      "AllowedChallengeTypes": {
        "dns": ["dns-01"],
        "ip": ["http-01"]
      }

      "ADCSOptions": {
        "CAServer": "CA.FQDN.com\\CA Name",
        "TemplateName": "Default-ACME-Template"
      }
    }
  }
```

Foreach identifier you can also define the allowed challenge types. This list shows all identifer types as well as their supported challenge types. Defaults are printed bold.

- dns, e.g www.example.com
  - **http-01**
  - **dns-01**
  - **tls-alpn-01**
  - dns-persist-01
- dns (wildcard), e.g *.example.com
  - **dns-01**
  - dns-persist-01
- ip, e.g. 10.94.95.96
  - **http-01**
  - **tls-alpn-01**
- permanent-identifier
  - **device-attest-01**


The profile selection process will run the identifier validation and only select profiles which match the parameters, e.g. if you want to use different CAs depending on DNS names, you could do something like this:

```json
  "Profiles": {
    "DNS-A": {
      "SupportedIdentifiers": [ "dns" ],

      "IdentifierValidation": {
        "DNS": {
          "AllowedDNSNames": [ ".sub-a.example.com" ]
        },
      }

      "ADCSOptions": {
        "CAServer": "CA.FQDN.com\\CA Name",
        "TemplateName": "DNS-A-ACME-Template"
      }
    },
    "DNS-B": {
      "SupportedIdentifiers": [ "dns" ],

      "IdentifierValidation": {
        "DNS": {
          "AllowedDNSNames": [ ".sub-b.example.com" ]
        },
      }

      "ADCSOptions": {
        "CAServer": "CA.FQDN.com\\CA Name",
        "TemplateName": "DNS-B-ACME-Template"
      }
    }
  }
```

A profile for device-attest-01 challenges could look like this:
Device-Attest-01 is a little bit more involved, since it allows remote validation via an [POST reqeuest](./device-attest.md) and needs to be configured with the Apple root certificate.
Currently, the device-attest-01 challenge is not standardized, so this profile is experimental and may change in the future - also it only supports the Apple device-attest-01 challenges.

If you are interested in android support or tpm support, please open an issue on the GitHub repository.

```json
  "Profiles": {
    "DeviceAttestProfile": {
      "SupportedIdentifiers": [ "permanent-identifier" ],
      "RequireExternalAccountBinding": true,
      "IdentifierValidation": {
        "PermanentIdentifier": {
          "ValidationRegex": "^[a-zA-Z0-9]{32,64}$"
        }
      },
      "ChallengeValidation": {
        "DeviceAttest01": {
          "RemoteValidationUrl": "https://device-attest-validation.example.com",
          "Apple": {
            "RootCertificates": [
              "MIICJDCC...gN/r"
            ]
          }
        }
      },
      "ADCSOptions": {
        "CAServer": "CA.FQDN.com\\CA Name",
        "TemplateName": "Device-Attest-Template"
      }
    }
  }
```
