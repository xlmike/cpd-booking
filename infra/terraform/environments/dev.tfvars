# Non-secret settings for the dev environment. alert_email comes from TF_VAR_alert_email.
environment               = "dev"
location                  = "uksouth"
resource_group_name       = "rg-cpd-dev"
app_service_sku           = "B1"
sql_sku                   = "Basic"
sql_admin_group_name      = "sg-cpd-sql-admins"
sql_admin_group_object_id = "fc182a52-75c4-4ac7-b5a3-fa76c332b217" # replace with the value bootstrap.sh prints
monthly_budget_gbp        = 20
