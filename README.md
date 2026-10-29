# StudentBank

A student banking coursework prototype built with ASP.NET Core, SQLite, cookie-based authentication, and admin-only reporting. It is for demonstration with fictional data, not a real financial service.

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
- `MyApp/` — .NET MAUI StudentBank coursework prototype with sign-in, authenticated demo accounts/transfers, read-only TrueLayer sandbox data, transaction insights with explicit consent, market quotes and AI explanations; Week 3/4 workshop pages are retained
- `docs/DESIGN_THINKING_WORKSHEET.md` — draft user-centred design worksheet for both apps
- `docs/WEEK_6_PROPOSAL_CRITIQUE_PITCH.md` — Week 6 proposal, project alignment, draft priorities, technical risks, phased plan, and pitch outline
- `docs/COURSE_WEEK_ALIGNMENT.md` — course materials shared so far mapped to the project, with evidence gaps by week

To run the Android MAUI app, configure provider keys on the server, start the StudentBank backend listening on port 5080, and then run `~/.dotnet/dotnet build -t:Run -f net10.0-android` in `MyApp/` with an Android emulator running. See `MyApp/DEVELOPMENT_NOTES.md` for provider setup. Internal account balances are fictional coursework ledger data stored in SQLite. TrueLayer access uses its sandbox, read-only scopes, and explicit provider consent; no external bank payments are made. Market quotes depend on provider entitlements and may be delayed.

The worksheet in `docs/` is a draft. It labels user needs as hypotheses and includes tasks for gathering real student feedback; it has not been copied into the linked Miro board or validated with participants.

The Week 6 proposal document summarizes the slide requirements and maps them to the current prototype. It includes unvalidated user stories, a draft MoSCoW list, an architecture diagram, and a phased plan. Interview evidence, peer critique, and the authenticated mobile account-read spike are still outstanding.

## Notes

The app stores data in a local SQLite database file named `studentbank.db` in the project folder so data persists between restarts. The database file is local and is not included in this repository.
