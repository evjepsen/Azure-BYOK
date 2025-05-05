# Azure BYOK implementation


## Setup

### Log in with `az`

You have to login with the `az` CLI tool, like:
```bash
az login 
```


### Set environment variables
You have to set up the following variables:

- `VaultUri`
- `SubscriptionId`
- `ResourceGroupName`
- `KeyVaultResourceName`
- `AllowedEmails`
- `SigningCertificateName`
- `ValidSubject`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Secret`

They are read from the appsettings.azure.json, so you have create one in the `API` directory

When setting up the `AllowedEmails`, you can use a comma separated list of emails. The above fields need to be included in either ´appsettings.azure.json´ or ´appsettings.json´ - the location doesn't really matter. They just have to present.

Example ´appsettings.azure.json´ file:

```json
{
  "ApplicationOptions" : {
    "VaultUri" : "https://xxx.vault.azure.net/",
    "SubscriptionId" : "xxx-xxx-xxx-xxx",
    "ResourceGroupName" : "xxx",
    "KeyVaultResourceName" : "xxx",
    "AllowedEmails": [
      "john.doe@gmail.com",
      "kahn@example.com"
    ],
    "SigningCertificateName": "BYOK-Signing-Certificate",
    "ValidSubject" : "cn=Customer HSM"
    "Jwt" : {
      "Issuer": "BYOK",
      "Audience": "xxx",
      "Secret": "xxx"
    }
  }
}
```


