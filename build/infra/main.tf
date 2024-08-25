### Setup
terraform {
  backend "azurerm" {
    resource_group_name  = "Landing-Zones-Default"
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

variable "laws_name" {
  type = string
  default = "lawsogdvtst"
} 

variable "b2c_authority" {
  type = string
} 

variable "b2c_issuer" {
  type = string
} 

variable "b2c_client_id" {
  type = string
} 

### Locals

locals {
  landingZoneRg = "Landing-Zones-Default"
  globalRg = "rgglobal"
  region = "westeurope"
  suffix = "squaregrid${lower(var.environment)}"
  tags = {
    Application      = "SquareGrid"
    Owner            = "Our Game (Michael & John Law)"
    Environment      = var.environment
    SharedResource   = "No"
    TerraformManaged = "Yes"
  }
}
 
### Imports 

data "azurerm_client_config" "current" {}

data "azurerm_log_analytics_workspace" "logs" {
  name                = var.laws_name
  resource_group_name = local.landingZoneRg
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
  workspace_id        = data.azurerm_log_analytics_workspace.logs.id
  tags = local.tags
}

resource "azurerm_storage_account" "storage" {
  name                     = "st${local.suffix}"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = local.region
  account_tier             = "Standard"
  account_replication_type = "LRS"
  allow_nested_items_to_be_public = false
  public_network_access_enabled = true
  tags = local.tags
}

####### Compute

resource "azurerm_service_plan" "plan" {
  name                = "plan-${local.suffix}"
  location            = local.region
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Linux"
  sku_name            = "Y1"
  tags                = local.tags
}

resource "azurerm_linux_function_app" "api" {
  name                       = "fa${local.suffix}"
  location                   = local.region
  resource_group_name        = azurerm_resource_group.rg.name
  service_plan_id            = azurerm_service_plan.plan.id
  storage_account_access_key = azurerm_storage_account.storage.primary_access_key
  storage_account_name       = azurerm_storage_account.storage.name
  tags                       = local.tags

  app_settings = {
    "WEBSITE_RUN_FROM_PACKAGE" = "1"  
    "BlobStorageConnection" = azurerm_storage_account.storage.primary_connection_string,
    "B2CAuthority": var.b2c_authority,
    "B2CIssuer": var.b2c_issuer,
    "B2CClientId": var.b2c_client_id,
  }

  site_config {
    application_insights_key                = azurerm_application_insights.insights.instrumentation_key
    application_insights_connection_string  = azurerm_application_insights.insights.connection_string
    websockets_enabled                      = true
    http2_enabled                           = true
    use_32_bit_worker                       = true

    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }
}

### Output

output "instrumentation_key" {
  value = azurerm_application_insights.insights.instrumentation_key
  sensitive = true
}

output "function_app_name" {
  value = azurerm_linux_function_app.api.name
}