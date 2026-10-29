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
- `MyApp/` — .NET MAUI StudentBank mobile prototype with Home, Accounts, and AI Assistant tabs; Week 3/4 workshop pages are retained
- `docs/DESIGN_THINKING_WORKSHEET.md` — draft user-centred design worksheet for both apps
- `docs/WEEK_6_PROPOSAL_CRITIQUE_PITCH.md` — Week 6 proposal, project alignment, draft priorities, technical risks, phased plan, and pitch outline
- `docs/COURSE_WEEK_ALIGNMENT.md` — course materials shared so far mapped to the project, with evidence gaps by week

To run the Android MAUI app, start the StudentBank backend with `OPENAI_API_KEY` configured, listening on port 5080, then change to `MyApp/` and run `~/.dotnet/dotnet build -t:Run -f net10.0-android` with an Android emulator running. The mobile interface uses fictional sample balances and is not connected to the bank database; its general-purpose assistant sends only the user's question to the configured AI service. See `MyApp/DEVELOPMENT_NOTES.md` for setup and course notes.

The worksheet in `docs/` is a draft. It labels user needs as hypotheses and includes tasks for gathering real student feedback; it has not been copied into the linked Miro board or validated with participants.

The Week 6 proposal document summarizes the slide requirements and maps them to the current prototype. It includes unvalidated user stories, a draft MoSCoW list, an architecture diagram, and a phased plan. Interview evidence, peer critique, and the authenticated mobile account-read spike are still outstanding.

## Notes

The app stores data in a local SQLite database file named `studentbank.db` in the project folder so data persists between restarts. The database file is local and is not included in this repository.
