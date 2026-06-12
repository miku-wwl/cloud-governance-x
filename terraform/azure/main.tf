data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false

  keepers = {
    subscription_id = data.azurerm_client_config.current.subscription_id
    name_prefix     = var.name_prefix
    environment     = var.environment
  }
}

locals {
  compact_environment = replace(var.environment, "-", "")
  suffix              = random_string.suffix.result

  common_tags = merge(
    {
      owner       = var.owner
      environment = var.environment
      cost-center = var.cost_center
      managed-by  = "terraform"
      project     = "cloud-governance-x"
    },
    var.additional_tags
  )
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${var.name_prefix}-${var.environment}-${local.suffix}"
  location = var.location
  tags     = local.common_tags
}

resource "azurerm_storage_account" "main" {
  name                     = "st${var.name_prefix}${local.compact_environment}${local.suffix}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"

  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = true

  blob_properties {
    versioning_enabled = true
  }

  tags = local.common_tags
}

resource "azurerm_servicebus_namespace" "main" {
  name                = "sb-${var.name_prefix}-${var.environment}-${local.suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "Basic"

  minimum_tls_version = "1.2"

  tags = local.common_tags
}

resource "azurerm_servicebus_queue" "governance_events" {
  name         = "governance-events"
  namespace_id = azurerm_servicebus_namespace.main.id

  max_delivery_count                   = 10
  lock_duration                        = "PT1M"
  default_message_ttl                  = "P14D"
  dead_lettering_on_message_expiration = true
}

resource "azurerm_log_analytics_workspace" "main" {
  count = var.enable_log_analytics ? 1 : 0

  name                = "log-${var.name_prefix}-${var.environment}-${local.suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = var.log_analytics_retention_days

  tags = local.common_tags
}
