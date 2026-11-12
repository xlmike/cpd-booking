terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Remote state in Azure Storage, with locking. Values are passed with -backend-config
  # (see infra/scripts/bootstrap.sh). use_azuread_auth: no storage account keys.
  backend "azurerm" {
    container_name   = "tfstate"
    key              = "cpd-booking-dev.tfstate"
    use_azuread_auth = true
  }
}

provider "azurerm" {
  features {}
  # Credentials come from the environment: ARM_USE_OIDC, ARM_CLIENT_ID, ARM_TENANT_ID,
  # ARM_SUBSCRIPTION_ID in GitHub Actions, or `az login` on your machine.
  storage_use_azuread = true

  # The pipeline identity only has rights on one resource group, so it can't register resource
  # providers for the whole subscription. bootstrap.sh registers the ones we need instead.
  resource_provider_registrations = "none"
}
