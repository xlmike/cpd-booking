# Security

This is a practice project. If you find a vulnerability, please open a private security advisory
on GitHub (Security tab → Report a vulnerability) rather than a public issue.

Controls in this repository:

- CodeQL static analysis on every pull request and weekly
- Dependabot updates and dependency review on pull requests
- GitHub secret scanning and push protection (enable in repository settings)
- No stored credentials: GitHub signs in to Azure with OpenID Connect, and the app reaches
  Azure SQL with a managed identity
