data "azurerm_resource_group" "main" {
  name = var.resource_group_name
}

resource "random_string" "suffix" {
  length  = 5
  upper   = false
  special = false
}

locals {
  name = "cpd-${var.environment}"
  # Globally unique names (web app, SQL server) get a short random suffix.
  unique = "${local.name}-${random_string.suffix.result}"
  tags = {
    service     = "cpd-booking"
    environment = var.environment
    managed_by  = "terraform"
  }
}

# ---------- Observability ----------

resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.name}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  daily_quota_gb      = 0.5 # cost guard for a practice environment
  tags                = local.tags
}

resource "azurerm_application_insights" "main" {
  name                = "appi-${local.name}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  tags                = local.tags
}

# ---------- Web app ----------

resource "azurerm_service_plan" "main" {
  name                = "asp-${local.name}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = var.app_service_sku
  tags                = local.tags
}

resource "azurerm_linux_web_app" "main" {
  name                = "app-${local.unique}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true

  # No FTP or basic-auth publishing: deployments only through the pipeline's Entra ID sign-in.
  ftp_publish_basic_authentication_enabled       = false
  webdeploy_publish_basic_authentication_enabled = false
  client_affinity_enabled                        = false

  # The app signs in to Azure SQL as itself. No password exists anywhere.
  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                         = var.app_service_sku != "F1"
    minimum_tls_version               = "1.2"
    ftps_state                        = "Disabled"
    http2_enabled                     = true
    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 5

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                     = "Production"
    "APPLICATIONINSIGHTS_CONNECTION_STRING"      = azurerm_application_insights.main.connection_string
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3" # auto-instrumentation: no SDK in the code
    "Persistence__Provider"                      = "SqlServer"
    "Persistence__MigrateOnStartup"              = "true"
  }

  connection_string {
    name  = "CpdBooking"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azurerm_mssql_database.main.name};Authentication=Active Directory Default;Encrypt=True;"
  }

  logs {
    detailed_error_messages = false
    failed_request_tracing  = false

    application_logs {
      file_system_level = "Information"
    }

    http_logs {
      file_system {
        retention_in_days = 3
        retention_in_mb   = 35
      }
    }
  }

  tags = local.tags
}

# ---------- Database ----------

resource "azurerm_mssql_server" "main" {
  name                          = "sql-${local.unique}"
  location                      = var.location
  resource_group_name           = data.azurerm_resource_group.main.name
  version                       = "12.0"
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true # see the firewall note below

  # Entra ID only: SQL logins and passwords are switched off.
  azuread_administrator {
    login_username              = var.sql_admin_group_name
    object_id                   = var.sql_admin_group_object_id
    azuread_authentication_only = true
  }

  tags = local.tags
}

# "Allow Azure services" (0.0.0.0) lets the App Service reach SQL cheaply. It also admits other
# Azure tenants' services, so in production use VNet integration and a private endpoint instead,
# and set public_network_access_enabled = false. Access still needs an Entra ID identity.
resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_mssql_database" "main" {
  name                 = "sqldb-${local.name}"
  server_id            = azurerm_mssql_server.main.id
  sku_name             = var.sql_sku
  max_size_gb          = 2
  zone_redundant       = false
  storage_account_type = "Local" # backup redundancy: cheapest, fine for practice data
  tags                 = local.tags

  # In a real service, protect data from an accidental `terraform destroy`:
  # lifecycle { prevent_destroy = true }
}

# ---------- Alerts and cost control ----------

resource "azurerm_monitor_action_group" "email" {
  name                = "ag-${local.name}"
  resource_group_name = data.azurerm_resource_group.main.name
  short_name          = "cpdalerts"

  email_receiver {
    name          = "owner"
    email_address = var.alert_email
  }

  tags = local.tags
}

resource "azurerm_monitor_metric_alert" "server_errors" {
  name                = "alert-${local.name}-5xx"
  resource_group_name = data.azurerm_resource_group.main.name
  scopes              = [azurerm_linux_web_app.main.id]
  description         = "More than 5 server errors in 5 minutes."
  severity            = 2
  frequency           = "PT1M"
  window_size         = "PT5M"

  criteria {
    metric_namespace = "Microsoft.Web/sites"
    metric_name      = "Http5xx"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 5
  }

  action {
    action_group_id = azurerm_monitor_action_group.email.id
  }

  tags = local.tags
}

resource "azurerm_consumption_budget_resource_group" "main" {
  name              = "budget-${local.name}"
  resource_group_id = data.azurerm_resource_group.main.id
  amount            = var.monthly_budget_gbp
  time_grain        = "Monthly"

  time_period {
    start_date = formatdate("YYYY-MM-01'T'00:00:00Z", timestamp())
  }

  notification {
    enabled        = true
    threshold      = 80
    operator       = "GreaterThanOrEqualTo"
    contact_emails = [var.alert_email]
  }

  notification {
    enabled        = true
    threshold      = 100
    operator       = "GreaterThanOrEqualTo"
    threshold_type = "Forecasted"
    contact_emails = [var.alert_email]
  }

  lifecycle {
    ignore_changes = [time_period] # the start date is only needed when the budget is created
  }
}
