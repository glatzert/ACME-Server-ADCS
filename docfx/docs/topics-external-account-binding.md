# External Account Binding

External Account Binding (EAB) is a mechanism that allows you to associate an external account with your ACME account.
A common use case is preauthentication of the ACME client before it can issue certificates.
This is particularly useful in environments where you want to enforce additional security measures or track certificate issuance more closely.

Enabling EAB will allow you to also define profiles, that are restricted to accounts, that have successfully passed the EAB check.

An account sending EAB data cannot be validated by the ACME-ADCS server alone, since it needs the HMAC key to verify the signature.
To use EAB, you need to configure it in the `appsettings.Production.json` file under the `ExternalAccountBinding` section:

```jsonc
{
  // Other configuration settings...

  "AcmeServer": {
    // ...

    "ExternalAccountBinding": {

      // [Required] If false, EAB is optional for account creation.
      "Required": true,

      // [Required] Url to retrieve the MAC-key for given {kid} (see README about this)
      "MACRetrievalUrl": "https://myEABService.example.com/mac/{kid}",

      // [Optional] Url to signal successful EAB check
      "SuccessSignalUrl": "https://myEABService.example.com/success/{kid}",

      // [Optional] Url to signal failed EAB check
      "FailedSignalUrl": "https://myEABService.example.com/failed/{kid}",

      // [Optional] Http-Headers to be sent to the MAC related URLs
      "Headers": [
        {
          "Key": "Authorization",
          "Value": "ApiKey TrustmeBro"
        }
      ]
    }

    // ...
  }
}
```

You can set the `Required` property to `false` if you want to make EAB optional for account creation, but the MAC retrieval URL must still be provided.

The optional `SuccessSignalUrl` and `FailedSignalUrl` can be used to notify your MAC service about the outcome of the EAB check. This is useful for logging or auditing purposes.
The MAC retrieval URL must return the HMAC key for the given `kid` in the request, which is used to verify the EAB signature sent by the ACME client.

The ACME-ADCS server expects the key to be in the format of a base64url-encoded string.

The `Headers` array allows you to specify additional HTTP headers that should be sent with the requests to the MAC-related URLs. This can be useful for authentication or other purposes.

```txt
REQUEST:

GET https://myEABService.example.com/mac/eab-kid-here
HEADERS:
    Authorization: ApiKey TrustmeBro


RESPONSE:

200 OK
<base64Url-encoded-MAC>

```