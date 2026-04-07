# Service Account and ADCS Template

Before we can start with the deployment of ACME-ADCS, we'll need a service account and at least one template for certificate issuance.

## Service Account

Independent of the deployment model (IIS or windows service), the application needs to be run with an process identity allowing it to issue (and possibly revoke) certificates.

Create such an service account in your Microsoft Active Directory.  
I highly recommend using a [group managed service account](https://learn.microsoft.com/de-de/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview) or similar types of accounts, so passwords won't be a concern.

In this documentation the account will be called `acme-user`.

## ADCS Template

On the ADCS server, create one or more certificate template(s), that will be used by the ACME server.
The certificate templates need to have **auto-enrollment** enabled for `acme-user`.  
Also make sure, the templates allow RSA and ECDSA certificates to be issued.

If you want to allow the ACME server to revoke certificates via the ACME protocol, also make sure to allow the `acme-user` to revoke certificates.

The documentation will assume `acme-template` and some variants for RSA `acme-rsa-template` or ECDSA `acme-ecdsa-template` only templates.