# Planned renewals with ARI

ACME Renewal Information (ARI, RFC 9773) allows the server to suggest renewal windows to ACME clients.
In ACME-ADCS, you can configure global defaults and optional planned renewal windows under `AcmeServer:ARI` in `appsettings.Production.json`.

## ARI configuration section

```jsonc
{
  "AcmeServer": {
	"ARI": {
	  "RetryAfter": "08:00:00",
	  "DefaultRenewalStart": "25%",
	  "DefaultRenewalPeriod": "1.00:00:00",
	  "PlannedRenewals": [
		{
		  "NotBeforeOlderThan": "2025-01-01T00:00:00Z",
		  "RenewalStart": "2025-06-01T00:00:00Z",
		  "RenewalPeriod": "2.00:00:00",
		  "ProfileName": "Sample-Profile-Full-Options",
		  "ExplanationUrl": "https://status.example.com/planned-renewal"
		}
	  ]
	}
  }
}
```

## Settings

- `RetryAfter`: Value used for the `Retry-After` response header in renewal info responses.
- `DefaultRenewalStart`: Start offset for calculated renewal windows.
  - Percentage format, for example `"25%"`.
  - Fixed timespan format, for example `"30.00:00:00"` (30 days before expiration).
- `DefaultRenewalPeriod`: Duration of the suggested renewal window.
- `PlannedRenewals`: Optional overrides for specific certificate groups.

## PlannedRenewals matching behavior

Each planned renewal entry can define:

- `NotBeforeOlderThan` (required): the certificate `NotBefore` date must be older than this value.
- `RenewalStart` (required): start of the suggested renewal window.
- `RenewalPeriod` (optional): if omitted, `DefaultRenewalPeriod` is used.
- `ProfileName` (optional): restricts matching to certificates issued by that profile.
- `ExplanationUrl` (optional): URL returned to clients for human-readable context.

If multiple entries match, ACME-ADCS returns the first matching entry from the `PlannedRenewals` array.