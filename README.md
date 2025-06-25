# Azure BYOK implementation
This project is a proof-of-concept implementation of Bring Your Own Key (BYOK) using Azure Key Vault and Azure Active Directory (Azure AD). Developed as part of a Bachelor’s thesis in Computer Science, it demonstrates how organizations can enable their customers to maintain control over their encryption keys while using Microsoft Azure services.

This project is intended for educational purposes and serves as a foundation for further development in cloud security and key management solutions.

## Setup

### Log in withth `az`

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


