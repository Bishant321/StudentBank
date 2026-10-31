# Course week alignment

This map connects the course materials shared for this project with the current StudentBank repository. It describes where existing work belongs and what still needs evidence. The PDFs remain in the user's Downloads folder; this repository contains notes and project work, not copies of the lecture materials.

## Materials reviewed

- `week1.pdf` — cover identifies it as the Week 1 module launch.
- `chapter_4.pdf` — cover identifies client-server architecture.
- `Week 4 (1).pdf` — cover identifies it as Week 4.
- `chapter_5_supplementary.pdf` — Week 5 supplementary lecture slides on relational databases, NoSQL, and data management/synchronization.
- `FGCT6021 Assessment Brief(1).pdf` — 2026/27 practical skills assessment and reflective report brief.
- `FGCT6021 Proposal, Critique & Pitch-3.pptx.pdf` — Week 6 proposal, critique, and pitch materials.
- `Worksheet_filled (1).pdf` — one-page Persona Template; the entry fields appear blank. It says to use the persona for the idea pitched the previous week, but does not identify a week number on the page.
- Week 3 requirements shared in the conversation — MAUI pages, Shell navigation, layouts, styles, and the sample project activity.

## Where the work belongs

| Week / item | Course focus from the material shared | Existing StudentBank work | Alignment and remaining action |
| --- | --- | --- | --- |
| Week 1 | Module launch and app idea | The chosen coursework concept is a StudentBank mobile banking prototype with an educational assistant. The repository also contains the web banking prototype. | The project has a clear direction. Keep the problem and target audience provisional until students have been interviewed; the project idea alone is not research evidence. |
| Week 3 | MAUI app shell, layouts, pages, navigation, shared styles, and sample-project exploration | `MyApp/` contains Home, Accounts, Assistant, task pages, Shell routes, shared styles, and the Pokémon workshop page. The app has been shown running in an Android emulator. | This is the right home for Shell, XAML layout, and route notes in [`MyApp/DEVELOPMENT_NOTES.md`](../MyApp/DEVELOPMENT_NOTES.md). The actual workshop checklist is recorded there. |
| Week 4 | Client-server architecture and APIs | The MAUI Pokémon page uses a typed `HttpClient` service for PokéAPI. The banking client also calls authenticated ASP.NET endpoints for StudentBank demo accounts and transfers. The backend uses SQLite. | This demonstrates client/server calls, JSON models, authenticated requests, and error handling. The Android app build and end-to-end emulator flow still need a successful run. |
| Week 5 | Supplementary lecture: relational data, DBMS/schema/SQL, local vs cloud, synchronization, and NoSQL models | The backend stores users, bank accounts, and transactions in SQLite via Entity Framework Core. MAUI workshop tasks are stored locally as JSON. Provider and AI APIs also exchange JSON. | The relational database is the right fit for structured accounts and balance-changing demo transactions. JSON files and API JSON are data formats, not NoSQL databases. The project has no cloud database replication or synchronization feature; its backend uses a local SQLite file. NoSQL is material to compare, not a requirement to add another database. See the Week 5 notes in [`MyApp/DEVELOPMENT_NOTES.md`](../MyApp/DEVELOPMENT_NOTES.md). |
| Persona worksheet | Persona, user needs, goals, frustrations, and context of use | `Worksheet_filled (1).pdf` is a blank one-page persona template. | Put its content under user research and requirements, and link the eventual persona to the proposal's target audience. The exact week is not shown on the worksheet, so do not label it as a specific week without the class schedule. Fill it from actual student input or label it as a hypothesis. |
| Week 6 | Proposal, critique, usability, feasibility, scope, pitch, and a phased plan to Week 12 | [`WEEK_6_PROPOSAL_CRITIQUE_PITCH.md`](WEEK_6_PROPOSAL_CRITIQUE_PITCH.md) contains the proposal, architecture diagram, draft MoSCoW list, risk register, interview guide, anonymous response forms, plan, and pitch outline. | The proposal began before the mobile-to-backend account connection was implemented. The authenticated demo-account read and transfer flow now exist, but Android end-to-end verification remains open. User interviews, participant task observations, and peer critique also remain outstanding; the desk review is not student feedback. |

## Feedback on the current prototype

This is a desk review of the app files and screens, not feedback collected from classmates or friends. It can support the Week 6 critique preparation, but must not be reported as participant research.

| Critique prompt | Feedback | Lens / reason | Suggested action |
| --- | --- | --- | --- |
| I like | The Home screen puts the total balance and recent activity up front. The app labels balances as demo data, and the Assistant screen explains that it cannot access accounts or move money. | Nielsen: clear status and recognition; Norman: the interface explains the assistant boundary. | Keep these explanations visible and check with students whether they understand them. |
| I wish | The account and transfer screens made the demo boundary visible at the point of action, and the app showed a clear recovery path when the backend or sandbox provider is unavailable. | Norman: signifiers and feedback should match what the system actually does. | Keep demo-transfer labels visible, test loading/error states on the emulator, and verify that provider failures explain how to reconnect. |
| What if | The app introduced the connected-account screen as optional sandbox exploration after students understand the core demo ledger? | Agile/MoSCoW: protect the core task and defer higher-risk work until it is feasible. | Validate the account-understanding task with students before expanding the project toward real provider access. |

These recommendations are recorded in more detail in the [Week 6 desk review](WEEK_6_PROPOSAL_CRITIQUE_PITCH.md#desk-review-answers-and-design-notes-not-student-feedback). The peer critique table and participant response forms remain blank until real sessions happen.

## Week-by-week record available so far

```text
Week 1  App direction: StudentBank mobile banking prototype with a learning assistant
Week 2  No week-specific material provided for this review
Week 3  MAUI Shell, layouts, routes, styles, and sample project — in MyApp notes
Week 4  Client-server architecture and API practice — in MyApp notes
Week 5  Relational databases, NoSQL, and synchronization — in MyApp development notes
Week 6  Proposal, critique, scope, risks, pitch, and phased plan — in Week 6 proposal notes
Week 7+ No week-specific slide or brief provided for this review
```

“No material provided” means this map cannot verify that week; it does not mean the course has no requirements for it. The Week 6 slides describe later phases through Week 12, but those phases are a planning guide, not evidence that their deliverables are already complete.

## Next actions in course order

1. Confirm the Week 1 app idea and target user using the module brief and actual student conversations.
2. Keep the Week 3, Week 4, and Week 5 code explanations in the mobile development notes as the implementation portfolio.
3. Complete the persona worksheet with anonymized, real observations; until then, call it a draft hypothesis.
4. Gather the Week 6 interview and critique evidence, finish the Android end-to-end check, and revise the scope and plan from those results.
5. Add future week materials to this map only when they are provided, then record the corresponding implementation and evidence in the relevant project notes.

## Assessment brief alignment — FGCT6021, 2026/27

The brief is assessment criteria to plan against, not a command to turn the prototype into a production bank. It specifies a practical skills assessment worth 75% and a report worth 25%. The final artefact is expected to be a fully functioning, deployed mobile application with frontend/backend integration, data management, API use, server-side functionality, and offline capability, plus a self-directed extension. The assessment also expects user/context research, iteration and feedback, testing, professional workflow, accessibility, privacy/security consideration, and critical reflection.

| Brief requirement | Evidence in this repository now | Remaining work / status |
| --- | --- | --- |
| A defined theme, user need, audience, and research-informed position | StudentBank is framed as a student finance prototype; the design worksheet and proposal identify user needs as hypotheses. | Gather and document genuine research/feedback, then revise the position and features from that evidence. Do not present the draft hypothesis or desk review as user research. |
| Working mobile artefact integrating frontend and backend, with data management, APIs, server-side functions, and offline capability | MAUI pages call the ASP.NET backend for sign-in, demo accounts, statements, and internal demo transfers; the backend uses SQLite. The retained workshop task app stores tasks locally as JSON. TrueLayer is sandbox-only. | Complete a successful Android build and end-to-end walkthrough; the latest screen flows have not been fully verified on an emulator. Banking screens currently need a network connection; define, implement, and demonstrate an appropriate offline experience if required by the brief. |
| Workshop work plus a self-directed extension | The repository retains MAUI task/navigation and Pokémon API exercises. StudentBank banking screens and assistant features form the independent extension. | Explain which techniques came from workshops and which features are the extension; justify scope and feature choices with research, usability, and feasibility evidence. Verify the archive includes all required workshop code from teaching Weeks 3–9; only some weeks have been mapped from supplied materials so far. |
| Deployed application | Backend instructions currently run a local development server; there is no production deployment URL recorded. | Deploy the app/service as appropriate for the module, verify the deployed experience, and include the link and deployment evidence. The TrueLayer sandbox must remain labelled as test data. |
| Code archive containing all artefact code | Source is in this GitHub repository. | Create and inspect the required ZIP or RAR, including relevant workshop code and extension; no submission archive is recorded yet. |
| Reflective report, maximum 1,200 words | Week 6 proposal and development notes provide source material. | Write the separate report: explain workshop influence, mini-project extension, decisions/challenges, strengths/limitations, testing and learning; keep within 1,200 words and include evidence such as screenshots/logs. |
| Presentation and professional communication | A draft pitch outline exists in the Week 6 proposal notes. | Prepare the presentation and demo around the final working build; the brief’s submission checklist does not state a separate upload location for the presentation, so check myUCA guidance. |
| AI declaration and source credits | The brief requires an AI declaration and references for sources, APIs, libraries, assets, datasets, and documentation. | Complete the official AI declaration honestly and add/verify citations and attributions in submitted materials. AI assistance has been used during development and should be disclosed according to the form and module guidance. |
| Usability, accessibility, privacy, security, and maintenance | The prototype includes data-boundary notices and transaction-insight consent; provider keys are documented as server-side settings. | Test accessibility and error states, review security, and resolve or document dependency audit advisories before submission. The local HTTP setup and sandbox OAuth flow are coursework-only. |

**Confirmed deadline:** the student has confirmed that the correct deadline is **Monday 11 January 2027 at 15:00 GMT** (page 1). Page 5's 18 December 2026 date conflicts with this and should be treated as a brief error. The Module Assessment Rubric and UCA Generic Grading Criteria are referenced by the brief but were not included, so this map cannot estimate a grade.
