#!/usr/bin/env bash
# One-off setup, run once from your own machine after `az login`.
# Creates what Terraform itself needs before it can run:
#   1. a resource group for the app (Terraform deploys into it)
#   2. a storage account for Terraform state (Entra ID auth only, no keys)
#   3. an Entra ID app registration that GitHub Actions signs in as, using OIDC (no secret)
#   4. an Entra ID group to administer Azure SQL, with you in it
# Usage: ./infra/scripts/bootstrap.sh <github-owner>/<github-repo>
set -euo pipefail

REPO="${1:?Usage: bootstrap.sh <github-owner>/<github-repo>}"
LOCATION="uksouth"
APP_RG="rg-cpd-dev"
STATE_RG="rg-cpd-tfstate"
STATE_SA="stcpdtf$(openssl rand -hex 3)"   # must be globally unique, lowercase, 3-24 chars
APP_NAME="github-cpd-booking"
SQL_ADMIN_GROUP="sg-cpd-sql-admins"

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
ME=$(az ad signed-in-user show --query id -o tsv)

echo "==> Register the Azure resource providers this project uses"
for NS in Microsoft.Web Microsoft.Sql Microsoft.Insights Microsoft.OperationalInsights Microsoft.Consumption Microsoft.Storage; do
  az provider register --namespace "$NS" -o none
done

echo "==> Resource groups"
az group create -n "$APP_RG" -l "$LOCATION" --tags service=cpd-booking environment=dev -o none
az group create -n "$STATE_RG" -l "$LOCATION" --tags service=cpd-booking purpose=terraform-state -o none

echo "==> Terraform state storage"
az storage account create -n "$STATE_SA" -g "$STATE_RG" -l "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 \
  --allow-blob-public-access false --allow-shared-key-access false -o none
az storage account blob-service-properties update --account-name "$STATE_SA" -g "$STATE_RG" \
  --enable-versioning true --enable-delete-retention true --delete-retention-days 14 -o none
STATE_SA_ID=$(az storage account show -n "$STATE_SA" -g "$STATE_RG" --query id -o tsv)
# You need data-plane rights to create the container and to run Terraform locally.
az role assignment create --assignee "$ME" --role "Storage Blob Data Contributor" --scope "$STATE_SA_ID" -o none
echo "    waiting for the role assignment to take effect..." && sleep 30
az storage container create -n tfstate --account-name "$STATE_SA" --auth-mode login -o none

echo "==> Identity for GitHub Actions (OIDC, no client secret)"
CLIENT_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
az ad sp create --id "$CLIENT_ID" -o none 2>/dev/null || true
SP_ID=$(az ad sp show --id "$CLIENT_ID" --query id -o tsv)

for SUBJECT in "repo:${REPO}:ref:refs/heads/main" "repo:${REPO}:environment:dev" "repo:${REPO}:pull_request"; do
  NAME=$(echo "$SUBJECT" | tr ':/' '--' | cut -c1-120)
  az ad app federated-credential create --id "$CLIENT_ID" --parameters "{
    \"name\": \"${NAME}\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"${SUBJECT}\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" -o none
done

echo "==> Least-privilege roles: Contributor on the app resource group only, plus state access"
APP_RG_ID=$(az group show -n "$APP_RG" --query id -o tsv)
az role assignment create --assignee-object-id "$SP_ID" --assignee-principal-type ServicePrincipal \
  --role Contributor --scope "$APP_RG_ID" -o none
az role assignment create --assignee-object-id "$SP_ID" --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Contributor" --scope "$STATE_SA_ID" -o none

echo "==> Entra ID group to administer Azure SQL"
GROUP_ID=$(az ad group create --display-name "$SQL_ADMIN_GROUP" --mail-nickname "$SQL_ADMIN_GROUP" --query id -o tsv)
az ad group member add --group "$GROUP_ID" --member-id "$ME"

cat <<OUT

Done. Now add these in GitHub: Settings > Secrets and variables > Actions

  Secrets:
    AZURE_CLIENT_ID        = ${CLIENT_ID}
    AZURE_TENANT_ID        = ${TENANT_ID}
    AZURE_SUBSCRIPTION_ID  = ${SUBSCRIPTION_ID}

  Variables:
    TF_STATE_RESOURCE_GROUP  = ${STATE_RG}
    TF_STATE_STORAGE_ACCOUNT = ${STATE_SA}
    ALERT_EMAIL              = <your email address>
    AZURE_WEBAPP_NAME        = <set after the first terraform apply>

And in infra/terraform/environments/dev.tfvars:
    sql_admin_group_object_id = "${GROUP_ID}"
OUT
