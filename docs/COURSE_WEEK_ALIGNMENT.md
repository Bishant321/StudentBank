# Course week alignment

This map connects the course materials shared for this project with the current StudentBank repository. It describes where existing work belongs and what still needs evidence. The PDFs remain in the user's Downloads folder; this repository contains notes and project work, not copies of the lecture materials.

## Materials reviewed

- `week1.pdf` — cover identifies it as the Week 1 module launch.
- `chapter_4.pdf` — cover identifies client-server architecture.
- `Week 4 (1).pdf` — cover identifies it as Week 4.
- `FGCT6021 Proposal, Critique & Pitch-3.pptx.pdf` — Week 6 proposal, critique, and pitch materials.
- `Worksheet_filled (1).pdf` — one-page Persona Template; the entry fields appear blank. It says to use the persona for the idea pitched the previous week, but does not identify a week number on the page.
- Week 3 requirements shared in the conversation — MAUI pages, Shell navigation, layouts, styles, and the sample project activity.

## Where the work belongs

| Week / item | Course focus from the material shared | Existing StudentBank work | Alignment and remaining action |
| --- | --- | --- | --- |
| Week 1 | Module launch and app idea | The chosen coursework concept is a StudentBank mobile banking prototype with an educational assistant. The repository also contains the web banking prototype. | The project has a clear direction. Keep the problem and target audience provisional until students have been interviewed; the project idea alone is not research evidence. |
| Week 3 | MAUI app shell, layouts, pages, navigation, shared styles, and sample-project exploration | `MyApp/` contains Home, Accounts, Assistant, task pages, Shell routes, shared styles, and the Pokémon workshop page. The app has been shown running in an Android emulator. | This is the right home for Shell, XAML layout, and route notes in [`MyApp/DEVELOPMENT_NOTES.md`](../MyApp/DEVELOPMENT_NOTES.md). The actual workshop checklist is recorded there. |
| Week 4 | Client-server architecture and APIs | The MAUI Pokémon page uses a typed `HttpClient` service for PokéAPI. The Assistant page sends a question to the ASP.NET `/api/assistant/chat` endpoint. The web banking API uses SQLite. | API and server examples belong with the Week 4 notes. Important gap: mobile Home and Accounts still show local fictional values and are not connected to authenticated bank-account endpoints. |
| Persona worksheet | Persona, user needs, goals, frustrations, and context of use | `Worksheet_filled (1).pdf` is a blank one-page persona template. | Put its content under user research and requirements, and link the eventual persona to the proposal's target audience. The exact week is not shown on the worksheet, so do not label it as a specific week without the class schedule. Fill it from actual student input or label it as a hypothesis. |
| Week 6 | Proposal, critique, usability, feasibility, scope, pitch, and a phased plan to Week 12 | [`WEEK_6_PROPOSAL_CRITIQUE_PITCH.md`](WEEK_6_PROPOSAL_CRITIQUE_PITCH.md) contains the proposal, architecture diagram, draft MoSCoW list, risk register, interview guide, anonymous response forms, plan, and pitch outline. | The planning material belongs here. User interviews, participant task observations, peer critique, and the authenticated mobile account-read spike remain outstanding. The desk review is explicitly not student feedback. |

## Feedback on the current prototype

This is a desk review of the app files and screens, not feedback collected from classmates or friends. It can support the Week 6 critique preparation, but must not be reported as participant research.

| Critique prompt | Feedback | Lens / reason | Suggested action |
| --- | --- | --- | --- |
| I like | The Home screen puts the total balance and recent activity up front. The app labels balances as demo data, and the Assistant screen explains that it cannot access accounts or move money. | Nielsen: clear status and recognition; Norman: the interface explains the assistant boundary. | Keep these explanations visible and check with students whether they understand them. |
| I wish | The Accounts screen says “Updated just now,” although its values are static examples. Send, Add, and Cards look like banking actions before the demo warning appears. | Norman: signifiers and feedback should match what the system actually does. | Remove or qualify the freshness text; label the quick actions as previews or make their demo status visible before tapping. |
| What if | The app focused its first release on one clear task: understand sample account balances and recent activity, then made live account reading a separate, tested milestone? | Agile/MoSCoW: protect the core task and defer higher-risk work until it is feasible. | Validate the core task with students, then run the authenticated read-only API spike before expanding scope. |

These recommendations are recorded in more detail in the [Week 6 desk review](WEEK_6_PROPOSAL_CRITIQUE_PITCH.md#desk-review-answers-and-design-notes-not-student-feedback). The peer critique table and participant response forms remain blank until real sessions happen.

## Week-by-week record available so far

```text
Week 1  App direction: StudentBank mobile banking prototype with a learning assistant
Week 2  No week-specific material provided for this review
Week 3  MAUI Shell, layouts, routes, styles, and sample project — in MyApp notes
Week 4  Client-server architecture and API practice — in MyApp notes
Week 5  No week-specific slide or brief provided for this review
Week 6  Proposal, critique, scope, risks, pitch, and phased plan — in Week 6 proposal notes
Week 7+ No week-specific slide or brief provided for this review
```

“No material provided” means this map cannot verify that week; it does not mean the course has no requirements for it. The Week 6 slides describe later phases through Week 12, but those phases are a planning guide, not evidence that their deliverables are already complete.

## Next actions in course order

1. Confirm the Week 1 app idea and target user using the module brief and actual student conversations.
2. Keep the Week 3 and Week 4 code explanations in the mobile development notes as the implementation portfolio.
3. Complete the persona worksheet with anonymized, real observations; until then, call it a draft hypothesis.
4. Gather the Week 6 interview and critique evidence, run the account-read feasibility spike, and revise the scope and plan from those results.
5. Add future week materials to this map only when they are provided, then record the corresponding implementation and evidence in the relevant project notes.
