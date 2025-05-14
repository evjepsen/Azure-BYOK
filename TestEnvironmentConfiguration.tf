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

variable "signing_cert_name" {
  type        = string
  description = "The Signing certificate name."
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

# Create virtual network and subnet
resource "azurerm_virtual_network" "vnet" {
  name                = "BYOK-VNET"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_subnet" "endpoint_subnet" {
  name                 = "PrivateEndpoint-Subnet"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.3.0/24"]
}

resource "azurerm_subnet" "internal" {
  address_prefixes     = ["10.0.2.0/24"]
  name                 = "Shared-BYOK-Subnet"
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name

  service_endpoints = ["Microsoft.KeyVault"]

  delegation {
    name = "app-service-delegation"

    service_delegation {
      name    = "Microsoft.Web/serverFarms"
      actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
    }
  }
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
  name                       = "BYOK-CUSTOMER-KV"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "premium"
  soft_delete_retention_days = 7
  purge_protection_enabled   = true
  enable_rbac_authorization  = true

  network_acls {
    bypass                     = "AzureServices"
    default_action             = "Deny"
    virtual_network_subnet_ids = [azurerm_subnet.internal.id]
    ip_rules                   = ["185.45.22.131/32"]
  }
}

resource "azurerm_role_assignment" "kv_cert_officer" {
  scope                = azurerm_key_vault.vault.id
  role_definition_name = "Key Vault Certificates Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

resource "azurerm_key_vault_certificate" "signing_cert" {
  key_vault_id = azurerm_key_vault.vault.id
  name         = var.signing_cert_name

  certificate_policy {
    issuer_parameters {
      name = "Self"
    }

    key_properties {
      exportable = false
      key_type   = "RSA-HSM"
      key_size   = 2048
      reuse_key  = false
    }

    lifetime_action {
      action {
        action_type = "AutoRenew"
      }
      trigger {
        days_before_expiry = 30
      }
    }

    secret_properties {
      content_type = "application/x-pkcs12"
    }

    x509_certificate_properties {
      subject = "CN=Customer HSM"
      key_usage = [
        "digitalSignature",
      ]

      validity_in_months = 12

    }
  }

  depends_on = [azurerm_role_assignment.kv_cert_officer]
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

  logs {
    detailed_error_messages = true
    failed_request_tracing  = true

    application_logs {
      file_system_level = "Information"
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"   = "Production"
    "WEBSITE_RUN_FROM_PACKAGE" = "1"

    "ApplicationOptions__VaultUri"               = azurerm_key_vault.vault.vault_uri
    "ApplicationOptions__SubscriptionId"         = var.subscription_id
    "ApplicationOptions__ResourceGroupName"      = azurerm_resource_group.rg.name
    "ApplicationOptions__KeyVaultResourceName"   = azurerm_key_vault.vault.name
    "ApplicationOptions__AllowedEmails"          = jsonencode(["ej@csep.dk", "lord64hacker@gmail.com", "davidm.bernardo@gmail.com"])
    "ApplicationOptions__SigningCertificateName" = var.signing_cert_name
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

# Integrate App Service with Virtual Network
resource "azurerm_app_service_virtual_network_swift_connection" "vnet_integration" {
  app_service_id = azurerm_windows_web_app.webapp-middleware.id
  subnet_id      = azurerm_subnet.internal.id
}

# Create a private DNS zone for Key Vault
resource "azurerm_private_dns_zone" "keyvault_dns" {
  name                = "privatelink.vaultcore.azure.net"
  resource_group_name = azurerm_resource_group.rg.name
}

# Link the DNS zone to the virtual network
resource "azurerm_private_dns_zone_virtual_network_link" "dns_link" {
  name                  = "keyvault-dns-link"
  resource_group_name   = azurerm_resource_group.rg.name
  private_dns_zone_name = azurerm_private_dns_zone.keyvault_dns.name
  virtual_network_id    = azurerm_virtual_network.vnet.id
  registration_enabled  = false
}

# Create a private endpoint for the Key Vault
resource "azurerm_private_endpoint" "kv_private_endpoint" {
  name                = "kv-private-endpoint"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  subnet_id           = azurerm_subnet.endpoint_subnet.id

  private_service_connection {
    name                           = "kv-connection"
    private_connection_resource_id = azurerm_key_vault.vault.id
    is_manual_connection           = false
    subresource_names              = ["vault"]
  }

  private_dns_zone_group {
    name                 = "kv-dns-grouop"
    private_dns_zone_ids = [azurerm_private_dns_zone.keyvault_dns.id]
  }
}

# Create a role assigment for the web app to access the key vault
resource "azurerm_role_assignment" "webapp-role-assigment-contributor" {
  principal_id         = azurerm_windows_web_app.webapp-middleware.identity[0].principal_id
  scope                = azurerm_key_vault.vault.id
  role_definition_name = "Contributor"
}

resource "azurerm_role_assignment" "webapp-role-assigment-cert-officer" {
  principal_id         = azurerm_windows_web_app.webapp-middleware.identity[0].principal_id
  scope                = azurerm_key_vault.vault.id
  role_definition_name = "Key Vault Certificates Officer"
}

resource "azurerm_role_assignment" "webapp-role-assigment-crypto-officer" {
  principal_id         = azurerm_windows_web_app.webapp-middleware.identity[0].principal_id
  scope                = azurerm_key_vault.vault.id
  role_definition_name = "Key Vault Crypto Officer"
}

resource "azurerm_role_assignment" "webapp-role-assigment-user-access-administrator" {
  principal_id         = azurerm_windows_web_app.webapp-middleware.identity[0].principal_id
  scope                = azurerm_key_vault.vault.id
  role_definition_name = "User Access Administrator"
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

  logs {
    detailed_error_messages = true
    failed_request_tracing  = true

    application_logs {
      file_system_level = "Information"
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"   = "Production"
    "WEBSITE_RUN_FROM_PACKAGE" = "1"
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.dbServer.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.db.name};Persist Security Info=False;User ID=${azurerm_mssql_server.dbServer.administrator_login};Password=${random_password.admin_password[0].result};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
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