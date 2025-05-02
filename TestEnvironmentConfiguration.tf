terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=4.1.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~>3.0"
    }
  }
}

variable "subscription_id" {
  type        = string
  description = "The subscription id of Azure Subscription where the environment is being set up."
}

# Variables for the Azure Key Vault
variable "microsoft_client_id" {
  type        = string
  description = "The microsoft client id."
}

variable "microsoft_client_secret" {
  type        = string
  description = "The microsoft client secret."
}

variable "google_client_id" {
  type        = string
  description = "The google client id."
}

variable "google_client_secret" {
  type        = string
  description = "The google client secret."
}

variable "jwt_secret" {
  type        = string
  description = "The JWT secret."
}

# Configure the Microsoft Azure Provider
provider "azurerm" {
  features {}

  subscription_id = var.subscription_id
}

# Configure data point that gets the client configuration
data "azurerm_client_config" "current" {}

# Create a resource group
resource "azurerm_resource_group" "rg" {
  name     = "BYOK-RG"
  location = "North Europe"
}

# Create a log analytics workspace to store logs (both for the web app and key vault)
resource "azurerm_log_analytics_workspace" "ai" {
  location            = azurerm_resource_group.rg.location
  name                = "BYOK-AI"
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
}

# Create a key vault
resource "azurerm_key_vault" "vault" {
  name                       = "BYOK-KV"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "premium"
  soft_delete_retention_days = 7
  purge_protection_enabled   = false
}

# Create an app service plan for the middleware
resource "azurerm_service_plan" "appserviceplan" {
  name                = "webapp-asp-BYOK"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Windows"
  sku_name            = "B1"
}

resource "azurerm_windows_web_app" "webapp-middleware" {
  name                = "webapp-middleware-BYOK"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  service_plan_id     = azurerm_service_plan.appserviceplan.id
  depends_on          = [azurerm_service_plan.appserviceplan]
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    minimum_tls_version = "1.2"

    application_stack {
      dotnet_version = "v8.0"
      current_stack  = "dotnet"
    }

  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"   = "Production"
    "WEBSITE_RUN_FROM_PACKAGE" = "1"

    "ApplicationOptions__VaultUri"               = azurerm_key_vault.vault.vault_uri
    "ApplicationOptions__SubscriptionId"         = var.subscription_id
    "ApplicationOptions__ResourceGroupName"      = azurerm_resource_group.rg.name
    "ApplicationOptions__KeyVaultResourceName"   = azurerm_key_vault.vault.name
    "ApplicationOptions__AllowedEmails"          = "[\"ej@csep.dk\", \"lord64hacker@gmail.com\", \"davidm.bernardo@gmail.com\"]"
    "ApplicationOptions__SigningCertificateName" = "BYOK-SigningCert"
    "ApplicationOptions__ValidSubject"           = "cn=Customer HSM"

    # Authentication settings 
    "Authentication__Google__ClientId"        = var.google_client_id
    "Authentication__Google__ClientSecret"    = var.google_client_secret
    "Authentication__Microsoft__ClientId"     = var.microsoft_client_id
    "Authentication__Microsoft__ClientSecret" = var.microsoft_client_secret

    # JWT Settings
    "JWT__Secret"   = var.jwt_secret
    "JWT__Issuer"   = "https://webapp-middleware-byok.azurewebsites.net"
    "JWT__Audience" = "https://webapp-middleware-byok.azurewebsites.net"
  }
}

#  Deploy code from a public GitHub repo
resource "azurerm_app_service_source_control" "sourcecontrol-middleware" {
  app_id                 = azurerm_windows_web_app.webapp-middleware.id
  repo_url               = "https://github.com/evjepsen/Azure-BYOK"
  branch                 = "master"
  use_manual_integration = true
  use_mercurial          = false
}

# Create a database for the fake ERP system
resource "random_password" "admin_password" {
  count       = 1
  length      = 20
  special     = true
  min_numeric = 1
  min_upper   = 1
  min_lower   = 1
  min_special = 1
}

resource "azurerm_mssql_server" "dbServer" {
  location                     = azurerm_resource_group.rg.location
  name                         = "byok-demo-erp-db-server"
  resource_group_name          = azurerm_resource_group.rg.name
  version                      = "12.0"
  administrator_login          = "byokadmin"
  administrator_login_password = random_password.admin_password[0].result
}

resource "azurerm_mssql_database" "db" {
  name           = "WeatherForecastDb"
  server_id      = azurerm_mssql_server.dbServer.id
  sku_name       = "Basic"
  zone_redundant = false
  max_size_gb    = 2
}

# Create a second web app for the fake ERP system
resource "azurerm_windows_web_app" "webapp-ERP" {
  name                = "webapp-ERP"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  service_plan_id     = azurerm_service_plan.appserviceplan.id
  depends_on          = [azurerm_service_plan.appserviceplan]
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    minimum_tls_version = "1.2"

    application_stack {
      dotnet_version = "v8.0"
      current_stack  = "dotnet"
    }

  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"   = "Production"
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Data Source=tcp:${azurerm_mssql_server.dbServer.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.db.name};User Id=${azurerm_mssql_server.dbServer.administrator_login};Password=${random_password.admin_password[0].result};Encrypt=true;TrustServerCertificate=false;MultipleActiveResultSets=true;"
  }
}

# Key Vault access policy for the web app's managed identity
resource "azurerm_key_vault_access_policy" "webapp_access" {
  key_vault_id = azurerm_key_vault.vault.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_windows_web_app.webapp-middleware.identity[0].principal_id

  key_permissions = [
    "Get", "List", "Sign", "Verify", "WrapKey", "UnwrapKey"
  ]

  secret_permissions = [
    "Get", "List"
  ]
}

# Setup the log analytics workspace for the key vault
resource "azurerm_monitor_diagnostic_setting" "kv_logs" {
  name                       = "kv_logs"
  target_resource_id         = azurerm_key_vault.vault.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.ai.id

  enabled_log {
    category_group = "allLogs"
  }

  enabled_log {
    category_group = "audit"
  }


  metric {
    category = "AllMetrics"
  }
}

# Output the password
output "admin_password" {
  sensitive = true
  value     = random_password.admin_password[0].result
}