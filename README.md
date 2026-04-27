# Insurance Management System API

Backend API implementation for the Insurance Management System challenge using `.NET 8`, `ASP.NET Core Web API`, `Entity Framework Core`, and `SQLite`.

## Setup

### Prerequisites

- .NET 8 SDK

### Run Locally

```powershell
dotnet build Insurance.Api.sln
dotnet run --project src/Insurance.Api/Insurance.Api.csproj
```

The API uses a local SQLite database at `src/Insurance.Api/insurance.db`.

### Run Tests

```powershell
dotnet test Insurance.Api.sln
```

### Swagger

After starting the API, open Swagger from the URL printed in the terminal, typically:

- `https://localhost:xxxx/swagger`
- `http://localhost:xxxx/swagger`

## Architecture

### High-level design

The solution follows a layered architecture with clear separation of concerns:

- `Controllers` handle HTTP request/response concerns and routing.
- `Services` contain business logic and validation rules.
- `Data` (`InsuranceDbContext` + EF configurations) handles persistence and entity mapping.
- `Contracts` define request/response DTOs for API boundaries.
- `Domain` contains core entities, enums, and custom exceptions.
- `Middleware` centralizes exception handling and standardizes API error contracts.

### Data modeling

Two core entities are modeled:

- **Customer**
  - `Id`, `FullName`, `Email`, `PhoneNumber`
  - One-to-many relationship to policies
- **Policy**
  - `Id`, `PolicyNumber`, `Type`, `Status`, `StartDate`, `EndDate`, `PremiumAmount`, `CustomerId`
  - Belongs to one customer

Key persistence constraints:

- Unique index on customer email (case-insensitive behavior in SQLite).
- Unique index on policy number.
- Foreign key from policy to customer with restricted delete behavior.

## Assumptions

- A customer email is unique system-wide and treated case-insensitively.
- A policy number is globally unique.
- Policy creation and update require an existing customer.
- Policy start date must be earlier than end date.
- Premium amount must be greater than zero.
- A customer cannot hold two **active** policies of the same policy type.
- Policy cancellation is only allowed from `Active` status.
- API error responses for business/domain failures use stable machine-readable codes with `400/404/409` semantics.

## Future Proofing (Scaling to 1M+ policies)

To scale this design to 1M+ policies, the next steps would be:

1. **Move to production-grade database**
   - Replace SQLite with PostgreSQL or SQL Server.
   - Use managed backups, replication, and high availability.

2. **Optimize query performance**
   - Add targeted indexes for frequent filters (`CustomerId`, `Type`, `Status`, date ranges).
   - Introduce pagination and sorting on list endpoints.
   - Avoid expensive full-table scans on large datasets.

3. **Strengthen API and service boundaries**
   - Add API versioning.
   - Introduce idempotency keys for create operations where needed.
   - Add richer observability (structured logs, metrics, tracing).

4. **Improve reliability and throughput**
   - Use background jobs/queues for non-critical workflows and async processing.
   - Add caching for read-heavy endpoints (where appropriate).
   - Scale horizontally behind a load balancer.

5. **Operational readiness**
   - Add migration strategy for zero/low-downtime deployments.
   - Expand automated test coverage (integration tests, contract tests).
   - Add security hardening (rate limiting, secrets management, audit logging).
