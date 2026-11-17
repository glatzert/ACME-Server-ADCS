# V3.1 to V3.2

There is a new feature (ARI RFC 9773) and a changed experimental feature dns-persist-01, that might need config adjustments.

# ARI (RFC 9773)

ARI allows planned renewals by certificate date and optionally issuance profile.  
Refer to appsettings-sample.json and look for ARI or look into the code listing on the [configuration page](./deployment-configuration.md).

# dns-persist-01 (Draft-02)

If you already used the dns-persist-01 challenge type, you'll need to adjust the config and provide some new values:

...

