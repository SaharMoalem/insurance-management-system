# Insurance API

Backend API skeleton for the Insurance assignment (`.NET 8`, controller-based Web API).

## Prerequisites

- .NET 8 SDK

## Run Locally

```powershell
dotnet restore Insurance.Api.sln
dotnet build Insurance.Api.sln
dotnet run --project src/Insurance.Api/Insurance.Api.csproj
```

## Run Tests

```powershell
dotnet test Insurance.Api.sln
```

## Swagger

After running, open the Swagger UI from the URL shown in terminal, typically:

- `https://localhost:xxxx/swagger`
- or `http://localhost:xxxx/swagger`

## Reviewer Verification Flow

1. Run API and open Swagger.
2. Create a customer via `POST /api/customers`.
3. Create a policy for that customer via `POST /api/policies`.
4. Validate error semantics:
   - duplicate customer email -> `409` with stable `error.code`
   - duplicate policy number -> `409` with stable `error.code`
   - missing resource lookup -> `404`
   - invalid payload/model -> `400` with predictable `error` object
5. Run `dotnet test Insurance.Api.sln` and confirm all tests pass.
