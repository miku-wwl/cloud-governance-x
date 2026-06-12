output "subscription_id" {
  description = "Azure subscription used by the deployment."
  value       = data.azurerm_client_config.current.subscription_id
}

output "resource_group_name" {
  description = "Name of the Day 2 resource group."
  value       = azurerm_resource_group.main.name
}

output "storage_account_name" {
  description = "Name of the StorageV2 account."
  value       = azurerm_storage_account.main.name
}

output "service_bus_namespace_name" {
  description = "Name of the Basic Service Bus namespace."
  value       = azurerm_servicebus_namespace.main.name
}

output "service_bus_queue_name" {
  description = "Name of the governance events queue."
  value       = azurerm_servicebus_queue.governance_events.name
}

output "log_analytics_workspace_name" {
  description = "Optional Log Analytics workspace name."
  value       = try(azurerm_log_analytics_workspace.main[0].name, null)
}

output "common_tags" {
  description = "Governance tags applied to all taggable resources."
  value       = local.common_tags
}
