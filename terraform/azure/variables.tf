variable "location" {
  description = "Azure region used by all Day 2 resources."
  type        = string
  default     = "australiaeast"
}

variable "name_prefix" {
  description = "Lowercase alphanumeric prefix used in globally unique resource names."
  type        = string
  default     = "finops"

  validation {
    condition     = can(regex("^[a-z0-9]{3,10}$", var.name_prefix))
    error_message = "name_prefix must contain 3-10 lowercase letters or digits."
  }
}

variable "environment" {
  description = "Deployment environment represented in names and tags."
  type        = string
  default     = "dev"

  validation {
    condition     = can(regex("^[a-z0-9-]{2,10}$", var.environment))
    error_message = "environment must contain 2-10 lowercase letters, digits, or hyphens."
  }
}

variable "owner" {
  description = "Owner tag used for governance and cost attribution."
  type        = string
  default     = "cloud-governance-x"
}

variable "cost_center" {
  description = "Cost center tag used for FinOps allocation."
  type        = string
  default     = "learning"
}

variable "enable_log_analytics" {
  description = "Creates a Log Analytics workspace when true."
  type        = bool
  default     = false
}

variable "log_analytics_retention_days" {
  description = "Log Analytics retention period when the optional workspace is enabled."
  type        = number
  default     = 30

  validation {
    condition     = var.log_analytics_retention_days >= 30 && var.log_analytics_retention_days <= 730
    error_message = "log_analytics_retention_days must be between 30 and 730."
  }
}

variable "additional_tags" {
  description = "Additional tags merged into the mandatory governance tags."
  type        = map(string)
  default     = {}
}
