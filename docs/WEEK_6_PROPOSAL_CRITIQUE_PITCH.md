# Week 6 proposal, critique and pitch

This document turns the Week 6 slide guidance into a working proposal for the StudentBank coursework prototype. Requirements below are summarized from the slides; project status is based on the repository as it stands. Suggested user needs and feedback are clearly marked as unvalidated.

## Slide guidance summary

The Week 6 checkpoint asks for evidence of research and development, real-user requirements, prototypes and navigation, frontend/backend/data architecture, a prioritized feature list, a defined audience, technical risks with early tests, and a phased plan through Week 12. The session frames the work around usability, feasibility, scope, critique, and a three-minute pitch.

The slides recommend testing the riskiest assumption first, collecting and acting on peer critique, using MoSCoW to prioritize scope, and ending each phase with something runnable. Before the next checkpoint, the stated independent practice is to complete a feedback log, refine the feature list and architecture, plan the phases, and run a technical spike. This document is a draft; it does not claim that interviews, critique, or usability tests have happened.

## Proposal

### One-line concept

**StudentBank helps students understand their money by presenting account balances and recent activity clearly, with an assistant for general budgeting and banking questions.**

This is a proposal, not a finding from user research.

### Intended audience and context

- **Proposed users:** students who want a clear view of their accounts and everyday money questions answered.
- **Proposed context:** checking a phone between study, travel, and other daily activities, sometimes with one hand or unreliable connectivity.
- **Evidence status:** this audience and context are assumptions carried forward from the project brief and design worksheet. Confirm them through short interviews before treating them as requirements.

### Problem and primary task

**Provisional problem:** students may have difficulty understanding their available money when balances, currencies, recent activity, or the meaning of an assistant's answers are unclear.

**Primary task to validate:** open the app and understand the balance and recent activity for an account; optionally ask a general budgeting question and understand what the assistant can and cannot access.

### User stories to validate

- As a student, I want to see each account balance with its currency so I can understand how much money is in each account.
- As a student, I want to review recent activity so I can recognize what has changed.
- As a student, I want to ask a general budgeting question so I can learn how to plan everyday spending.
- As a student, I want clear feedback about whether information is sample or current, and whether it is saved locally or on the server, so I know what the app is showing me.

These stories are hypotheses until discussed with students. A useful acceptance criterion for the prototype is: *Given I open the Home screen, when the account summary appears, then I can identify the currency and whether the values are sample data without asking for help.* Validate the wording and task with participants.

## Current prototype and architecture

The repository contains an ASP.NET Core banking coursework backend and a .NET MAUI mobile prototype. The mobile app now signs in to the backend and reads the signed-in user's demo ledger; internal transfers update only fictional balances in the local SQLite database. External account reading is a TrueLayer sandbox flow. Keep those demo and sandbox boundaries explicit in any pitch or demo.

```mermaid
flowchart LR
    User[Student] --> UI[MAUI XAML pages]
    UI --> State[Page code and view state]
    State --> BankingClient[Authenticated BankingApiClient]
    BankingClient --> Endpoint[ASP.NET account and demo transfer endpoints]
    Endpoint --> SQLite[(SQLite demo ledger)]
    State --> BankLink[TrueLayer sandbox read-only connection]
    State --> AssistantClient[Assistant and market API calls]
    AssistantClient --> EndpointAI[ASP.NET assistant endpoints]
    EndpointAI --> Model[OpenAI Responses API]
    WebUser[Web user] --> WebUI[Web dashboard]
    WebUI --> WebAPI[ASP.NET banking API]
    WebAPI --> SQLite[(SQLite database)]
```

The current mobile app has Home, Accounts, Assistant, and Markets tabs, a sign-in page and transfer route, plus retained Week 3 task pages and the Week 4 Pokémon API exercise. The backend has authenticated account, statement, and demo transfer endpoints. The mobile app also has optional, consent-gated transaction-category insights. TrueLayer access is sandbox-only; market quotes are informational and no trades are placed. See [mobile development notes](../MyApp/DEVELOPMENT_NOTES.md) and [design-thinking worksheet](DESIGN_THINKING_WORKSHEET.md) for implementation details.

## Prioritized scope (draft)

| Priority | Feature | Reason / boundary |
| --- | --- | --- |
| Must | Home and Accounts screens identify each fictional balance and currency clearly | Delivers the demo's core account-understanding task; label all displayed data as sample |
| Must | Show recent sample activity and clear loading, empty, and error states where a request occurs | Users need feedback and should not have to guess whether something worked |
| Must | Assistant explains general budgeting and banking concepts, with an explicit privacy boundary | Keep it educational; do not send account details or perform financial actions |
| Must | Three-tab navigation and accessible labels, scalable text, and large touch targets | Supports predictable use on a phone |
| Implemented; verify on device | Connect mobile demo-ledger views and internal demo transfers to the authenticated StudentBank API | Implemented for the coursework backend; complete emulator walkthrough and error-state review |
| Implemented; verify on device | Provide a visible offline status and read-only cached view of demo balances and statements | Cache uses MAUI SecureStorage per signed-in user; sign-out clears it; create, connect, and transfer actions are disabled while ledger reads are offline |
| Could | Add a simple spending summary based on clearly explained data | Requires validated user value and careful handling of financial data |
| Won't for this coursework phase | Real payments, production banking integration, investment advice, or AI actions on accounts | Outside safe, feasible prototype scope |
| Won't for this coursework phase | Social features and elaborate personalization | Defer until core needs and time allow |

Revisit these priorities after user conversations and critique. “Must” does not mean production-ready banking.

## Usability and critique plan

Run a small think-aloud session with student participants using the emulator or a clickable prototype. Ask participants to complete tasks rather than tour screens. Record completion, errors, time or steps, hesitation, and comments. Do not fill the evidence table with invented results.

1. Find the balance for a named account and say which currency it uses.
2. Find a recent transaction and explain whether it is sample data.
3. Ask the assistant a basic budgeting question, then explain what account information it can access.
4. Navigate back to Home and find Accounts using the app's navigation.

Use accessibility checks alongside the task: text scaling, contrast, screen-reader labels, keyboard focus where applicable, and whether primary actions are easy to reach on a phone. The first design question to test is whether people can tell sample information from live or saved information.

### Research and usability evidence log

Use the guide below with three student participants. Ask for permission to take notes, avoid collecting account or other sensitive personal information, and label notes P1, P2, and P3. The friends suggested for these sessions are not named in this public project document. Replace the blank fields only with what each person actually says or does.

#### Short interview and prototype session

**Opening:** “I’m working on a student banking app prototype. I’d like to learn how you understand the screens, not test you. There are no right answers. You can skip a question or stop at any time. May I take anonymous notes?”

Ask these questions before showing the app:

1. What do you usually want to check first when you open a banking app?
2. Tell me about the last time it was hard to understand a balance or transaction.
3. When you check money on your phone, where are you and what are you doing?
4. What would you expect a banking assistant to know about your account? What should it never do?

Then give the participant the phone and ask them to:

1. Find a balance and tell you its currency and whether they think it is sample or live data.
2. Find a recent transaction and explain what they think it means.
3. Ask the assistant a general budgeting question, then say what information they think it can access.
4. Return to Home and find the Accounts screen.

While they work, stay quiet. Record what they do, where they pause, any errors, and their words as accurately as possible. Afterward ask: “What was clear?”, “What was confusing?”, and “If you could change one thing, what would you change?” Do not suggest answers during the task.

| Participant code | Date | What they said about their needs (short notes or exact brief quotes) | Task result / hesitation observed | Change they suggested | Permission to use anonymous notes? |
| --- | --- | --- | --- | --- | --- |
| P1 |  |  |  |  |  |
| P2 |  |  |  |  |  |
| P3 |  |  |  |  |  |

The sessions have not been conducted yet. Do not describe the proposed questions or likely answers as participant feedback.

#### Desk-review answers and design notes (not student feedback)

These notes answer the interview topics only from the app files and screens. They describe the current prototype; they do not tell us what Ramesh, Shiva, Bibashu, or other students think.

| Topic | What the current prototype shows | Analysis / next step |
| --- | --- | --- |
| What can a user check first? | Home loads the signed-in user's StudentBank demo-ledger accounts from the ASP.NET API. It adds balances only when they share a currency; with mixed currencies it says “Multiple currencies.” Recent activity is assembled from account statements. | This avoids treating different currencies as one total. Ask participants whether the account summary and currency labels are clear. |
| Is the information sample or live? | Home labels the values as the StudentBank demo ledger and says they are not an external bank balance. Accounts labels its ledger as a coursework demo; the optional TrueLayer connection is explicitly a sandbox. | Keep these boundaries visible. The offline state identifies the save time and warns that changes are disabled. Check whether participants understand the different data sources. |
| Can a user understand recent activity? | Home shows the latest four transactions returned by the signed-in user's StudentBank demo statements, with description, date/type, currency, and amount. | The activity is from the coursework ledger, not an external bank. Ask participants whether they can tell that from the screen and whether transaction labels are clear. |
| What actions appear available? | Send opens an internal demo transfer page. Add and Cards show a coursework-demo notice and do not perform those actions. Transfers update only the StudentBank demo ledger; offline mode disables changes. | Keep the demo boundary in the transfer confirmation and evaluate whether users understand that no real money moves. |
| What does the assistant claim and receive? | The Assistant screen says it cannot access accounts or move money and warns users not to share secrets. The server request contains the typed question and does not attach account balances or identifiers. | The boundary is stated clearly in the interface and code. Verify through a participant task whether the wording is understood; also test the unconfigured-server and connection-error states. |
| Does the current code show real student context or usability? | No interviews, participant observations, task-completion results, or peer critique are present in the repository. | The app files cannot establish where students use banking apps, what they need first, or whether they find this prototype easy. Collect those answers directly and then update the blank P1–P3 forms. |

**Design-review summary:** the prototype separates the StudentBank demo ledger from the TrueLayer sandbox, keeps the assistant's account-access boundary visible, and now provides a timestamped, read-only offline ledger cache. These observations come from source review, not user validation. Test whether people understand the labels and cached-data warning.

### Peer critique log

Record feedback using “I like / I wish / What if,” name the design lens, then decide whether to accept, adapt, or reject it with a reason. No peer feedback has been supplied yet.

| Feedback | Lens | Decision | Rationale and next action |
| --- | --- | --- | --- |
| Not collected yet |  |  |  |

## Feasibility and early technical spike

### Known evidence

- The MAUI app has been shown running in an Android emulator, which supports the basic cross-platform UI and navigation feasibility. The latest authenticated screens and offline fallback still need a recorded end-to-end walkthrough.
- The repository contains an ASP.NET API, SQLite-backed account and transfer operations, and separate assistant endpoints.
- The mobile assistant service targets the local development backend. A public release would need a secured HTTPS backend and operational controls.
- Mobile account screens request the signed-in user's StudentBank coursework ledger; this is not the user's external bank account. TrueLayer uses sandbox data and is read-only here.
- Account summaries and statements are cached in per-user MAUI SecureStorage for offline viewing. Writes and provider connection controls are disabled when the account read falls back to the cache. This behavior still needs device verification, including sign-out and reconnect cases.

### Riskiest assumption and spike

**Assumption tested in implementation:** the MAUI app can read the signed-in user's StudentBank demo account summary and recent activity from the existing ASP.NET API without sending account details to the general assistant.

**Spike status:** authenticated ledger and statement calls, account rendering, demo transfers, and a bounded offline cache are implemented. The assistant's general chat sends the typed question; transaction insights are separately consent-gated and aggregate categories/counts. Still verify the signed-in flow, failure handling, cache isolation, sign-out clearing, and assistant request boundaries on the Android emulator.

| Risk | Likelihood | Impact | Early check / mitigation |
| --- | --- | --- | --- |
| Mobile authenticated ledger flow or offline behavior fails on device | Medium | High | Complete Android sign-in, read, offline fallback, reconnect, sign-out, and demo-transfer walkthrough; record issues and fix them |
| API or AI service is unavailable, slow, or rate-limited | Medium | High | Show retry/error feedback; retain clearly labelled sample data for the demo; test timeout and no-key behavior |
| Assistant output is mistaken for personal financial advice or account-aware guidance | Medium | High | State its limits in the UI and response instructions; test prompts about account access, transfers, fraud, and advice |
| Sensitive account data or API credentials leak to the app or repository | Medium | High | Keep provider keys server-side; minimize mobile data; inspect logs and requests; never commit secrets |
| Scope exceeds the remaining schedule | High | Medium | Keep the Must list small and defer Should/Could features until the core task works |
| Android works but an iOS build cannot be demonstrated | Medium | Medium | Prioritize emulator demonstration; confirm Mac/iOS tooling early if iOS is required |

## Phased development plan to Week 12

Each phase should end with a runnable build. Adjust dates and estimates with the actual module calendar and available hours.

| Phase | Weeks | Deliverable | Done when… |
| --- | --- | --- | --- |
| 0. Walking skeleton | Week 6 | Existing MAUI shell, API project, and local prototype documented; authenticated demo-ledger reads and offline fallback implemented | App runs on emulator and signed-in, offline, and reconnect behavior are demonstrated |
| 1. Core MVP | Week 7 | Refine Home and Accounts screens, data-source labels, essential navigation, and feedback from student research | A student can complete the primary account-understanding task in a short usability session |
| 2. Intelligent feature | Week 8 | Educational assistant with visible limits and server-side configuration | Assistant handles expected questions and failure cases without receiving account details |
| 3. Test and refine | Weeks 9–10 | Usability findings, defect list, accessibility refinements, updated critique log | Priority defects are fixed and at least one design change is checked with users |
| 4. Release preparation | Weeks 11–12 | Stable build, setup and architecture notes, demonstration flow, submission evidence | A clean checkout can be built and the demo can be completed reliably |

The Week 6 slide deck places the proposal, critique log, revised MoSCoW list, architecture, and phased plan in the portfolio. The plan above is a draft based on the deck; confirm dates against the course calendar.

## Three-minute pitch draft

Use approximately five slides with readable text and show the screens. Replace the unvalidated claims with evidence after speaking to students.

1. **0:00–0:20 — One liner:** “StudentBank helps students understand their money by presenting account balances and activity clearly, with an assistant for general budgeting questions.”
2. **0:20–0:50 — User and problem:** identify the student audience and phone context; say that the need is a hypothesis until interviews are completed.
3. **0:50–1:40 — Core task:** demonstrate Home, Accounts, and the Assistant boundary; point out sample-data labels.
4. **1:40–2:20 — Feasibility:** show the MAUI-to-ASP.NET authenticated demo-account flow and SQLite ledger; explain that TrueLayer is sandbox-only and live banking access would need a regulated provider arrangement.
5. **2:20–2:50 — Scope and plan:** show Must/Should/Won't and Weeks 6–12 phases.
6. **2:50–3:00 — Ask:** request critique on whether the account summary is clear and whether connecting mobile views to the authenticated API should outrank other features.

## Next actions

1. Interview a few students and replace the proposed needs with what they actually say and do.
2. Complete and record a successful Android emulator walkthrough of sign-in, account read, and demo transfer; keep any observed failures in the technical risk list.
3. Present the prototype, capture peer critique, and fill the feedback log with accept/adapt/reject decisions.
4. Refine the Must/Should/Could/Won't scope and Week 6–12 estimates using that evidence.
