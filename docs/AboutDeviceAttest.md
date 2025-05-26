# Device-Attest-01

Device Attestation is a challenge type defined in the [ACME Device Attestation draft](https://www.ietf.org/archive/id/draft-acme-device-attest-03.html).
It is used to issue certificates for devices that support Apple's DeviceCheck or Android's SafetyNet attestation services or TPM based attestation.

It is currently in draft state and not yet standardized, but it is supported (at least) by Apple devices and can be used to issue certificates for these devices within ACME-ADCS.
To use this you'll need some MDM solution, like Microsoft Intune, to manage the devices and to provide the attestation service.

Since the ACME-ADCS server cannot fully verify the attestation besides it being properly signed and 'fresh', ACME-ADCS allows to contact a remote API to verify the attestation.

// explain the remote API call, expected response, etc.