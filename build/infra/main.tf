### Setup
terraform {
  backend "azurerm" {
    resource_group_name  = "Landing-Zones-Default"
    key="SquareGridLive.tfstate"
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
  subscription_id = "a3ac85e7-ff10-4e73-b806-3ab91af8f0c4"
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

variable "dns_zone_name" {
  type = string
  default = "squaregrid.org"
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

data "azurerm_dns_zone" "dns_zone" {
  name                = var.dns_zone_name
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
  public_network_access_enabled = true
  tags = local.tags

  static_website {
    index_document     = "index.html"
    error_404_document = "404.html"
  }
}

resource "azurerm_storage_queue" "redirect" {
  name                 = "redirects"
  storage_account_name = azurerm_storage_account.storage.name
}

####### DNS

resource "azurerm_cdn_profile" "cdn_profile" {
  name                = "cdn${local.suffix}"
  location            = local.region
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard_Microsoft"
}

resource "azurerm_cdn_endpoint" "cdn_endpoint" {
  name                = "cdnendpoint${local.suffix}"
  profile_name        = azurerm_cdn_profile.cdn_profile.name
  resource_group_name = azurerm_resource_group.rg.name
  location            = local.region
  is_http_allowed     = true
  is_https_allowed    = true
  origin_host_header = "${azurerm_storage_account.storage.primary_web_host}"
  
  origin {
    name      = "storage-origin"
    host_name = "${azurerm_storage_account.storage.primary_web_host}"
  }
}

resource "azurerm_dns_cname_record" "link" {
  name                = "go"
  zone_name           = data.azurerm_dns_zone.dns_zone.name
  resource_group_name = data.azurerm_dns_zone.dns_zone.resource_group_name
  ttl                 = 300
  target_resource_id  = azurerm_cdn_endpoint.cdn_endpoint.id
}

resource "azurerm_cdn_endpoint_custom_domain" "link" {
  name            = "linkdns"
  cdn_endpoint_id = azurerm_cdn_endpoint.cdn_endpoint.id
  host_name       = "${azurerm_dns_cname_record.link.name}.${data.azurerm_dns_zone.dns_zone.name}"

  cdn_managed_https {
    certificate_type = "Dedicated"
    protocol_type    = "ServerNameIndication"
    tls_version      = "TLS12"
  }
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
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE" = "true"  
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

  lifecycle {
    ignore_changes = [
      auth_settings_v2, 
      app_settings["WEBSITE_RUN_FROM_PACKAGE"]
    ]
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