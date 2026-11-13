-- Run once after the first `terraform apply`, connected to the app database as a member of the
-- sg-cpd-sql-admins group (Azure portal > SQL database > Query editor, signed in with Entra ID).
-- Replace app-cpd-dev-xxxxx with the web app name from `terraform output web_app_name`.
--
-- Creates a database user for the web app's managed identity. No password is involved.
-- db_ddladmin lets the app apply EF Core migrations at start-up. In production, run migrations
-- from the pipeline instead and give the app only db_datareader and db_datawriter.

CREATE USER [app-cpd-dev-wr8l4] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [app-cpd-dev-wr8l4];
ALTER ROLE db_datawriter ADD MEMBER [app-cpd-dev-wr8l4];
ALTER ROLE db_ddladmin   ADD MEMBER [app-cpd-dev-wr8l4];
