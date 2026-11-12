variable "environment" {
  description = "Short environment name, used in resource names."
  type        = string
  validation {
    condition     = contains(["dev", "test", "prod"], var.environment)
    error_message = "environment must be dev, test or prod."
  }
}

variable "location" {
  description = "Azure region. UK South keeps data in the UK."
  type        = string
  default     = "uksouth"
}

variable "resource_group_name" {
  description = "Resource group created by bootstrap.sh. Terraform deploys into it, so its identity only needs rights on this group."
  type        = string
}

variable "app_service_sku" {
  description = "App Service plan size. B1 is the cheapest with Always On; F1 is free but sleeps."
  type        = string
  default     = "B1"
}

variable "sql_sku" {
  description = "Azure SQL Database size. Basic is about £4 a month."
  type        = string
  default     = "Basic"
}

variable "sql_admin_group_name" {
  description = "Display name of the Entra ID group that administers the SQL server."
  type        = string
}

variable "sql_admin_group_object_id" {
  description = "Object ID of that Entra ID group."
  type        = string
}

variable "alert_email" {
  description = "Where alerts and budget warnings go. Set as TF_VAR_alert_email (a GitHub variable), not in Git."
  type        = string
  sensitive   = true
}

variable "monthly_budget_gbp" {
  description = "Budget for the resource group. You're emailed at 80% and 100%."
  type        = number
  default     = 20
}
