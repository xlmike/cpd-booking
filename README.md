# Book a teacher training course (practice project)

A small .NET 10 service for practising modern government-style delivery on Azure: an ASP.NET Core
MVC site and REST API, built test-first, accessible to WCAG 2.2 AA with GOV.UK Frontend, secured
against the OWASP Top 10:2025, deployed by GitHub Actions with Terraform.

> This is a practice project, not a real government service. It deliberately doesn't use the
> GOV.UK crown or the GDS Transport font, which are only for GOV.UK services.

## What's in it

| Path | What it is |
| --- | --- |
| `src/CpdBooking.Domain` | Business rules (`Course.Book`). No framework dependencies. |
| `src/CpdBooking.Application` | Use cases (`BookingService`, `CourseCatalogue`) and the `ICourseRepository` port. |
| `src/CpdBooking.Infrastructure` | In-memory repository (local and tests) and the booking reference generator. |
| `src/CpdBooking.Infrastructure.SqlServer` | EF Core repository for Azure SQL, with optimistic concurrency. |
| `src/CpdBooking.Web` | MVC pages, `/api/v1` API, security headers, rate limiting, health check. |
| `tests/CpdBooking.UnitTests` | xUnit tests for the rules and use cases. |
| `tests/CpdBooking.IntegrationTests` | `WebApplicationFactory` tests: API, CSRF, headers, error pages. |
| `tests/accessibility` | Playwright + axe tests for WCAG 2.2 AA and keyboard use. |
| `infra/terraform` | App Service, Azure SQL (Entra ID only), App Insights, alerts, budget. |
| `infra/scripts` | One-off bootstrap (state storage, OIDC identity) and SQL access script. |
| `.github` | CI, CodeQL, Terraform pipeline, Dependabot, CODEOWNERS, PR template. |

## Run it locally

Requirements: .NET 10 SDK, Node.js 22.

```bash
npm ci && npm run build          # GOV.UK Frontend assets and CSS
dotnet test                      # unit and integration tests
dotnet run --project src/CpdBooking.Web
# open https://localhost:7080 — the API is at /api/v1/courses

npx playwright install chromium  # first time only
npm run test:a11y                # accessibility tests (starts the app itself)
```

Locally the app uses in-memory data. Set `Persistence__Provider=SqlServer` and a
`ConnectionStrings__CpdBooking` to use SQL Server.

## Database migrations

```bash
dotnet tool restore
dotnet ef migrations add InitialCreate \
  --project src/CpdBooking.Infrastructure.SqlServer \
  --startup-project src/CpdBooking.Infrastructure.SqlServer
```

## Deploy to Azure

See the step-by-step guide. In short: run `infra/scripts/bootstrap.sh <owner>/<repo>`, add the
printed secrets and variables to GitHub, open a PR to see the Terraform plan, merge to apply,
grant the app's identity access to SQL, then every merge to `main` builds, tests and deploys.

## API

| Method | Path | Result |
| --- | --- | --- |
| GET | `/api/v1/courses?search=` | 200, upcoming courses |
| GET | `/api/v1/courses/{id}` | 200, or 404 problem details |
| POST | `/api/v1/courses/{id}/bookings` `{ "email": "..." }` | 201; 400 invalid email; 404; 409 full, closed or already booked; 429 rate limited |

## DecisionsSee `docs/adr`.