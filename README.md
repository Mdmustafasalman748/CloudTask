
# CloudTask

![CI](https://github.com/Mdmustafasalman748/CloudTask/actions/workflows/ci.yml/badge.svg)

Enterprise task management system built with .NET 8, EF Core, and SQL Server.

Users can create projects and assign tasks, with JWT authentication and role-based authorization.

## Tech Stack

- .NET 8 Web API
- ASP.NET Core MVC (frontend)
- Entity Framework Core + SQL Server
- ASP.NET Core Identity
- JWT Authentication and Role-Based Authorization
- ASP.NET Core Rate Limiting
- xUnit
- GitHub Actions CI/CD
- Azure deployment

## Authentication and Authorization

- **Register:** `POST /api/auth/register` creates a domain profile and an Identity login in one transaction. New users are always assigned the `Member` role.
- **Login:** `POST /api/auth/login` returns a signed JWT (60-minute expiry) containing `sub` (user ID), `email`, and `role` claims.
- **Credential Security:** Incorrect email and incorrect password return the same `401 Unauthorized` response to prevent user enumeration.
- **Account Lockout:** Accounts are locked after 5 failed login attempts.
- **Rate Limiting:** Login requests are rate limited per IP address to help protect against brute-force attacks. Requests exceeding the configured limit receive `429 Too Many Requests`.
- **Authorization:** Authorization is layered:
  - `[Authorize]` requires a valid JWT (401 Unauthorized).
  - Role-based authorization restricts admin endpoints (403 Forbidden).
  - Ownership checks restrict unauthorized project modifications (403 Forbidden).
- **Secrets Management:** Sensitive configuration values, including `Jwt:Key` and database connection strings, are stored in .NET User Secrets during local development and environment variables in Azure. Secrets are never committed to Git.

## Testing

- xUnit integration tests for API endpoints.
- Test database support for SQL Server and EF Core InMemory.
- JWT-authenticated test clients.
- Configurable rate limits for integration testing.

## CI/CD and Deployment

- GitHub Actions workflow for automated builds and tests.
- Azure deployment.

## Status

Work in progress. Building one day at a time.
