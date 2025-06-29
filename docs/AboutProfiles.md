# Profiles (or issuance profiles)

Profiles are used to define rules and settings for specific types of certificate requests and orders in ACME-ADCS via `appsettings.Production.json`.
There are essentially two ways to select a profile: 
	0. via the `profile` query parameter in the ACME client request
	0. by the ACME-ADCS server, based on the identifiers used in the certificate order

A profile contains the supported identifier types, validation rules and the settings for issuing certificates.
The following profile would allow issuing DNS and IP certificates for any account, without any special restrictions:

```json
  "Profiles": {
    // A sample for a DNS and IP profile, the name 'default' is arbitrary, you can choose any name you like.
    "Default": {
      "SupportedIdentifiers": [ "dns", "ip" ],

      "ADCSOptions": {
        "CAServer": "CA.FQDN.com\\CA Name",
        "TemplateName": "Default-ACME-Template"
      }
    }
  }
```

A profile for device-attest-01 challenges could look like this:
Device-Attest-01 is a little bit more involved, since it allows remote validation via an [POST reqeuest](./AboutDeviceAttest.md) and needs to be configured with the Apple root certificate.
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
