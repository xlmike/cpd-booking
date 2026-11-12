# 2. No stored credentials for Azure or the database

Date: 2026-11-08

## Status

Accepted

## Context

Stored secrets (service principal secrets, SQL passwords, connection strings with passwords) leak
through repositories, logs and pipelines, and have to be rotated. OWASP Top 10:2025 lists
cryptographic failures, misconfiguration and supply chain failures among the top risks.

## Decision

- GitHub Actions signs in to Azure with OpenID Connect federated credentials. There is no client secret.
- The web app reaches Azure SQL with its system-assigned managed identity
  (`Authentication=Active Directory Default`).
- Azure SQL allows Entra ID authentication only; SQL logins are switched off.
- Terraform state uses Entra ID authentication; storage account keys are disabled.
- The pipeline identity is Contributor on the app's resource group only (least privilege).

## Consequences

There are no secrets to rotate or leak. Setup needs a one-off bootstrap script and a one-off
SQL statement to create the app's database user. Moving to a private endpoint for SQL is the
next step for a production service.
