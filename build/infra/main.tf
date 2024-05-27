### Setup
terraform {
  backend "azurerm" {
    resource_group_name  = "Landing-Zones-Default"
    key                  = "SquareGrid.tfstate"
  }
  required_providers {
    azapi = {
      source = "Azure/azapi"
    }
  }
}
 
provider "azapi" {
}

provider "azurerm" {
  features {}
}

### Variables

variable "environment" {
  type = string
  default = "Dev"
} 

variable "ac_name" {
  type = string
}

variable "kv_name" {
  type = string
}

variable "db_username" {
  type = string
  sensitive = true
}

variable "db_password" {
  type = string
  sensitive = true
}

variable "db_size" {
  type = number
  default = 10
} 

variable "db_sku" {
  type = string
  default = "S0"
} 

### Locals

locals {
  landingZoneRg = "Landing-Zones-Default"
  region = "westeurope"
  suffix = "SquareGrid${lower(var.environment)}"
  tags = {
    Application      = "SquareGrid"
    Owner            = "Michael Law"
    Environment      = var.environment
    SharedResource   = "No"
    TerraformManaged = "Yes"
  }
}
 
### Imports 

data "azurerm_client_config" "current" {}

data "azurerm_key_vault" "kv" {
  name                = var.kv_name
  resource_group_name = local.landingZoneRg
}

data "azurerm_app_configuration" "config" {
  name                = var.ac_name
  resource_group_name = local.landingZoneRg
}

data "azurerm_function_app_host_keys" "api_keys" {
  name                = "fa${local.suffix}"
  resource_group_name = "rg${local.suffix}"

  depends_on = [azurerm_windows_function_app.api]
}

### Resources

resource "azurerm_resource_group" "rg" {
  name      = "rg${local.suffix}"
  location  = local.region
  tags      = local.tags
}

resource "azurerm_application_insights" "insights" {
  name                = "ai${local.suffix}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = local.region
  application_type    = "web"
  tags = local.tags
}

resource "azurerm_storage_account" "storage" {
  name                     = "st${local.suffix}"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = local.region
  account_tier             = "Standard"
  account_replication_type = "LRS"
  allow_nested_items_to_be_public = false
  tags = local.tags
}

### Identity

resource "azurerm_user_assigned_identity" "containerapps" {
  location            = azurerm_resource_group.rg.location
  name                = "uai-${local.suffix}"
  resource_group_name = azurerm_resource_group.rg.name

  tags = local.tags
}

####### Compute

resource "azurerm_service_plan" "plan" {
  name                = "plan-${local.suffix}"
  location            = local.region
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Windows"
  sku_name            = "Y1"
  tags                = local.tags
}

resource "azurerm_windows_function_app" "api" {
  name                       = "fa${local.suffix}"
  location                   = local.region
  resource_group_name        = azurerm_resource_group.rg.name
  service_plan_id            = azurerm_service_plan.plan.id
  storage_account_access_key = azurerm_storage_account.storage.primary_access_key
  storage_account_name       = azurerm_storage_account.storage.name
  tags                       = local.tags

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.containerapps.id]
  }

  app_settings = {
    "AppConfigUri"                                    = data.azurerm_app_configuration.config.endpoint
    "AppConfigLabel"                                  = var.environment
    "AZURE_CLIENT_ID"                                 = azurerm_user_assigned_identity.containerapps.client_id
    "WEBSITE_RUN_FROM_PACKAGE"                        = "1"
  }

  site_config {
    application_insights_key                = azurerm_application_insights.insights.instrumentation_key
    application_insights_connection_string  = azurerm_application_insights.insights.connection_string
    websockets_enabled                      = true
    http2_enabled                           = true
    use_32_bit_worker                       = true

    application_stack {
      dotnet_version              = "v6.0"
      use_dotnet_isolated_runtime = true
    }
  }
}

###### Database

resource "azurerm_mssql_server" "mssql" {
  name                         = "mssql-${local.suffix}"
  location                     = local.region
  resource_group_name          = azurerm_resource_group.rg.name
  version                      = "12.0"
  administrator_login          = var.db_username
  administrator_login_password = var.db_password

  tags = local.tags
}

resource "azurerm_mssql_database" "db" {
  name           = "db-${local.suffix}"
  server_id      = azurerm_mssql_server.mssql.id
  max_size_gb    = var.db_size
  license_type   = "LicenseIncluded"
  sku_name       = var.db_sku
  tags           = local.tags
}

resource "azurerm_mssql_firewall_rule" "azureresources" {
  name                = "azureresources"
  server_id           = azurerm_mssql_server.mssql.id
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "0.0.0.0"

  depends_on          = [azurerm_mssql_server.mssql]
}

### Config

resource "azurerm_key_vault_secret" "kvsstorage" {
  name         = "StorageConnection"
  value        = azurerm_storage_account.storage.primary_connection_string
  key_vault_id = data.azurerm_key_vault.kv.id
}

resource "azurerm_key_vault_secret" "kvsdbconnection" {
  name         = "SqlConnection"
  value        = "Server=tcp:${azurerm_mssql_server.mssql.fully_qualified_domain_name},1433;Database=${azurerm_mssql_database.db.name};Persist Security Info=False;User ID=${var.db_username};Password=${var.db_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  key_vault_id = data.azurerm_key_vault.kv.id
}

resource "azurerm_app_configuration_key" "acsqlconnection" {
  configuration_store_id = data.azurerm_app_configuration.config.id
  key                    = "SqlConnection"
  type                   = "vault"
  label                  = var.environment
  vault_key_reference    = azurerm_key_vault_secret.kvsdbconnection.versionless_id

  depends_on = [
    data.azurerm_app_configuration.config
  ]
}

resource "azurerm_app_configuration_key" "acstorageconnection" {
  configuration_store_id = data.azurerm_app_configuration.config.id
  key                    = "StorageConnection"
  type                   = "vault"
  label                  = var.environment
  vault_key_reference    = azurerm_key_vault_secret.kvsstorage.versionless_id
  
  depends_on = [
    data.azurerm_app_configuration.config
  ]
}

### IAM

resource "azurerm_role_assignment" "keyvault_reader" {
   scope                = data.azurerm_key_vault.kv.id
   role_definition_name = "Key Vault Secrets Officer"
   principal_id         = azurerm_user_assigned_identity.containerapps.principal_id
}

resource "azurerm_role_assignment" "app_config_reader" {
   scope                = data.azurerm_app_configuration.config.id
   role_definition_name = "App Configuration Data Reader"
   principal_id         = azurerm_user_assigned_identity.containerapps.principal_id
}

### Output

output "instrumentation_key" {
  value = azurerm_application_insights.insights.instrumentation_key
  sensitive = true
}

output "storage_name" {
  value = azurerm_storage_account.storage.primary_table_endpoint
}

output "storage_connection" {
  value = azurerm_storage_account.storage.primary_connection_string
  sensitive = true
}

output "app_config_uri" {
  value = data.azurerm_app_configuration.config.endpoint
}

output "function_app_name" {
  value = azurerm_windows_function_app.api.name
}