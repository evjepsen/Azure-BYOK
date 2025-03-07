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

They are read from the `.env` file using [dotnet-env](https://github.com/tonerdo/dotnet-env), so you have create one in the root of the project.

Example ´.env´ file:

```sh 
VAULT_URI="https://xxxxx.vault.azure.net/"
```


