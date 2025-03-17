# Azure BYOK implementation


## Setup

### Log in with `az`

You have to login with the `az` CLI tool, like:
```bash
az login 
```


### Set environment variables
You have to setup the following enviroment variables:

- `VAULT_URI`

They are read from the appsettings.azure.json, so you have create one in the `API` directory

Example ´appsettings.azure.json´ file:

```json
{
  "VAULT_URI" : "https://xxx.vault.azure.net/",
  "SUBSCRIPTION_ID" : "x",
  "RESOURCE_GROUP_NAME" : "x",
  "KV_RESOURCE_NAME" : "x"
}
```


