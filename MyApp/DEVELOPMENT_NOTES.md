# StudentBank mobile app

The MAUI project is now a mobile banking coursework prototype. Its Shell tabs lead to Home, Accounts, and Assistant. The balances and transactions are fictional display data: account actions do not move money and the phone app is not connected to the banking database.

## Mobile assistant setup

The Assistant tab calls `POST /api/assistant/chat` on the StudentBank ASP.NET server. The server sends only the typed question to the OpenAI Responses API; it does not attach balances, transactions, or account identifiers. The API key stays on the server and must be configured as `OPENAI_API_KEY`. Without that key, the app shows a setup message. Replies are general educational information and should not be treated as financial advice.

For the Android emulator, start the backend from the repository root so it listens on port 5080:

```sh
OPENAI_API_KEY="your-server-side-key" dotnet run --project WebApplication1 --urls http://0.0.0.0:5080
```

In a second terminal, start the Android app from `MyApp/`:

```sh
~/.dotnet/dotnet build -t:Run -f net10.0-android
```

The Android app uses cleartext HTTP to reach the local development server. This is for the emulator prototype; use HTTPS and authenticated, rate-limited infrastructure before any public deployment. The server currently limits assistant requests to 12 per minute.

## Week 3 shell and navigation

## App shell plan

| Page name | Layout choice | Key controls | Navigates to |
| --- | --- | --- | --- |
| StudentBank Home | `ScrollView`, `Grid`, `VerticalStackLayout`, and bindable transaction rows | Sample balance, quick actions, recent activity, Assistant shortcut | Accounts; Assistant |
| Accounts | `ScrollView` with account cards | Two fictional account balances and masked account references | Home; Assistant through Shell tabs |
| Assistant | `Grid` with prompt chips, `CollectionView` conversation, and message `Entry` | Learning prompts, loading feedback, general banking questions | Home; Accounts through Shell tabs |
| Tasks (workshop page) | `Grid` with heading, action row, and a `CollectionView` | Add task button, task cards, due date and status | Add Task; selecting a task opens Task Details |
| Add Task | `ScrollView` with `VerticalStackLayout` | Name `Entry`, description `Editor`, `DatePicker`, Save and Cancel | Save or Cancel returns to Tasks |
| Task Details | `VerticalStackLayout` with a small `Grid` for due date | Title, description, status, completion button, Back | Back returns to Tasks |
| Pokémon Finder | `Grid` with a search row and a `CollectionView` | Search `Entry`, Search button, result cards, error message | Workshop route retained for Week 4 practice |

```mermaid
flowchart TD
    Home[StudentBank Home] --> Accounts[Accounts]
    Home --> Assistant[Assistant]
    Accounts --> Assistant
    Tasks[Workshop Tasks list] -->|Add a task| Add[Add Task]
    Add -->|Save or Cancel| Tasks
    Tasks -->|Select a task with taskId| Detail[Task Details]
    Detail -->|Back| Tasks
    Tasks -->|Explore Pokémon| Pokemon[Pokémon Finder]
    Pokemon -->|Back| Tasks
```

## Sample project checklist

The two downloaded `maui-samples-main` zip files have the same SHA-256 hash, so one copy is enough. The notes below come from `10.0/Beginners-Series/BeginnersTask` in the zip; it was inspected in place without unpacking the full repository.

- `Platforms/` — Contains platform-specific startup code, manifests, and settings for Android, iOS, Mac Catalyst, Windows, and Tizen.
- `Resources/` — Holds shared app assets such as images, fonts, icons, splash art, and styles that MAUI packages for each target.
- `MauiProgram.cs` — Creates the MAUI app, registers `App`, configures fonts and services, and builds the dependency injection container.
- `App.xaml` and `App.xaml.cs` — Define shared application resources; the code-behind initializes them and opens the app shell in a window.
- `AppShell.xaml` and `AppShell.xaml.cs` — Declare the root `MainPage` route; the code-behind registers `DetailPage` as a navigation route.
- `TargetFrameworks` in `BeginnersTask.csproj` — Targets .NET 10 for Android, iOS, and Mac Catalyst, with Windows added conditionally; platform target frameworks control which builds are available.

## Workshop pages kept in the project

Tasks are stored as JSON in the app's private data folder and loaded on startup. Adding a task or changing its completion status writes the updated collection to disk. Detail navigation passes a `taskId` query parameter; `DetailPage` reads it with `QueryProperty` and looks up the selected task. The starter `.gitignore` excludes `bin/` and `obj/` build output.

## Week 4 API activity

The Pokémon Finder uses PokéAPI's `pokemon/{name-or-id}` endpoint. `Pokemon` and its nested type models map the JSON response. `PokemonService` calls `GetFromJsonAsync<Pokemon>()` through a typed `HttpClient`, registered in `MauiProgram.cs` with the API base address. The page displays results in an `ObservableCollection<Pokemon>` bound to a `CollectionView`, and reports not-found and connection errors. The Android manifest includes internet access permission. Search with a name such as `pikachu` or a Pokédex number such as `25`.

The page shows a progress indicator while searching, disables repeated searches, and gives the search field an accessibility description and hint. The PokéAPI page remains in the project as workshop practice, but Home is now the app's starting screen. The wider design-thinking worksheet for StudentBank and this app is in `docs/DESIGN_THINKING_WORKSHEET.md` at the repository root.

The system `dotnet` command may resolve to an SDK without MAUI workloads. The user-local SDK at `~/.dotnet/dotnet` has the Android MAUI workload.

## Week 6 proposal and feasibility work

See [`../docs/WEEK_6_PROPOSAL_CRITIQUE_PITCH.md`](../docs/WEEK_6_PROPOSAL_CRITIQUE_PITCH.md) for the proposal draft, architecture map, prioritized scope, critique and usability evidence templates, technical risk register, and Week 6–12 plan. The Android emulator run demonstrates the MAUI shell, but does not verify the mobile banking screens against the web banking database. The highest-priority open technical question is whether the MAUI client can securely read the signed-in user's accounts from the existing API. Record the result of that spike here when it has been run. Do not fill in interview or critique logs without actual participant feedback.
