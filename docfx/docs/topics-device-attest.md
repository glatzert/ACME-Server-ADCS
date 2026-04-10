# Device-Attest-01

Device Attestation is a challenge type defined in the [ACME Device Attestation draft](https://www.ietf.org/archive/id/draft-acme-device-attest-04.html).
It is used to issue certificates for devices that support Apple's DeviceCheck or Android's SafetyNet attestation services or TPM based attestation.

It is currently in draft state and not yet standardized, but it is supported (at least) by Apple devices and can be used to issue certificates for these devices within ACME-ADCS.
To use this you'll need some MDM solution, like Microsoft Intune, to manage the devices and to provide the attestation service.

To enable device-attest-01 and permanent-identifiers, you need to create a profile configuration in the `appsettings.Production.json`

```jsonc
{
  // Other configuration settings...

  // [Required] Issuance profile configuration - there's no default profile, you must define at least one.
  "Profiles": {

    // The profile name is used to identify the profile in the ACME-Server.
    // You may choose any arbitrary name, that contains only alphanumeric characters, dashes and underscores.
    "device-profile1": {

      // [Required] List of supported identifiers for this profile.
      // Possible valies are: dns, ip, permanent-identifier, hardware-module
      "SupportedIdentifiers": [ "permanent-identifier" ],

      // [Optional] If true, external account binding is required to use this profile. The default is false.
      "RequireExternalAccountBinding": false,

      // [Optional] The following settings are used to validate the identifiers.
      "IdentifierValidation": {

        // [Optional] Configure permanent-identifier validation
        "PermanentIdentifier": {

          // [Optional] ValidationRegex is used to restrict the permanent identifiers.
          // The default is .* - so no restriction.
          "ValidationRegex": ".*"
        }
      },

      // [Optional] The following settings are used configure challenge validation
      "ChallengeValidation": {

        // [Optional] Configuration for device-attest-01 challenges.
        "DeviceAttest01": {

          // [Optional] Enables remote validation of device-attest-01 challenges (see below)
          "RemoteValidationUrl": "https://device-attest-validation.example.com",

          // [Optional] Configuration for apple type device-attest-01 challenges.
          "Apple": {

            // [Optional] The root certificate for apple device-attest-01 challenges.
            // The default is the apple enterprise root attestation certificate from https://www.apple.com/certificateauthority/private/
            "RootCertificates": [
              "MIICJDCC...gN/r"
            ]
          }
        }
      },

      // [Required] The following settings are used to issue the certificate.
      "ADCSOptions": {
        // [Required] The CA-Server to use for certificate issuance.
        "CAServer": "CA.FQDN.com\\CA Name",

        // [Required] The template to use for certificate issuance.
        "TemplateName": "ACME-Device-Template"
      }
    },
```

Since the ACME-ADCS server cannot fully verify the attestation besides it being properly signed and 'fresh', ACME-ADCS optionally allows to contact a remote API to verify the attestation.
It will be a post request to the URL configured in the `RemoteValidationUrl` setting of the `DeviceAttest01` challenge validation configuration, that contains the following json-serialized data:

```txt
REQUEST:
	POST https://device-attest-validation.example.com
    BODY:
    {
        "account": {
            "id": "<the accounts id value>",
            "eab": "<the accounts eab or null>" // might be omitted if EAB is not used
        },

        challenge: {
            "type": "device-attest-01", // the challenge type
            "id": "<the challenge id>",  // the challenge identifier
            "payload": "<the original attestation object>" // the original attestation object as base64url-encoded string

            "identifier": { 
                "type": "permanent-identifier", 
                "value": "12345678-1234-1234-1324-123456789132"
            }
        }

        attestation: {
            "attestationFormat": "apple", // the attestation format, currently only apple is supported
            "identifier": { 
                "type": "permanent-identifier", 
                "value": "<the permanent identifier value>"
            },
            
            // The attestation certificate chain, starting with the attestation certificate and ending with the intermediate certificate.
            "certficates": [ 
                "<the attestation certificate in PEM format>", 
                "<the intermediate certificate in PEM format>" 
            ],

            "cred-cert-extensions": { // contains the extensions of the attestation certificate
                "<oid>": "<asn1-value>" // since the extensions can be arbitrary, they are serialized as a dictionary of OID to ASN.1 value
                ...
            }
        }
    }

RESPONSE
200 OK
BODY:
    {
        "IsValid": true
    }
```