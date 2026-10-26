# StudentBank

A secure ASP.NET Core banking system with a web front end, SQLite storage, cookie-based authentication, and admin-only reporting.

## Features

- User registration and login
- Account creation and management
- Deposits and withdrawals with balance validation
- Transfers between accounts
- Persistent storage in SQLite
- Admin dashboard with financial summary and all-account listing
- Swagger/OpenAPI support for API testing
- Browser-based front end served from `/index.html`

## Run locally

```bash
dotnet run --project WebApplication1
```

Then open:

- Web app: http://localhost:5000/index.html
- Swagger UI: http://localhost:5000/swagger
- Health check: http://localhost:5000/api/health

## Administrator setup

Set the `StudentBank__AdminEmail` and `StudentBank__AdminPassword` environment variables before starting the app to provision an administrator. Do not commit real credentials to source control.

## Example requests

Register a user:

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Alice Johnson","email":"alice@example.com","password":"Password123!"}'
```

Create an account after logging in:

```bash
curl -X POST http://localhost:5000/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"holderName":"Alice Johnson","initialDeposit":2500.00,"currency":"USD"}'
```

Transfer funds:

```bash
curl -X POST http://localhost:5000/api/transfer \
  -H "Content-Type: application/json" \
  -d '{"fromAccountNumber":"SB-12345678","toAccountNumber":"SB-23456789","amount":150.00,"description":"Rent payment"}'
```

## Project structure

- `WebApplication1/Program.cs` — API routes, identity, database, services, and auth logic
- `WebApplication1/wwwroot/index.html` — front-end banking dashboard
- `WebApplication1/appsettings.json` — connection string and configuration

## Notes

The app stores data in a local SQLite database file named `studentbank.db` in the project folder so data persists between restarts. The database file is local and is not included in this repository.
