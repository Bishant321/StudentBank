# User-centred design worksheet

This is a working draft for the technical session. It is based on the StudentBank prototype and the MAUI workshop app. The Miro board was not readable from this workspace, so copy this draft into the board and replace hypotheses with evidence from actual student feedback.

## 1. Empathize

### Intended users

- Students who need a simple view of their banking accounts and recent activity.
- Students who want to review example banking information and learn budgeting basics on a phone.

These are target-user assumptions, not findings from interviews.

### Questions to ask students

- What do you need to see first when you open a banking app?
- What makes you hesitate before sending money?
- How do you keep track of balances in different currencies?
- How do you currently remember coursework deadlines?
- What should a task app do when you lose connectivity or close it?
- Which labels, text sizes, or controls are difficult to use on your phone?

### Evidence to collect

Record the participant's words and observed actions separately. Do not enter invented answers.

| Participant (anonymous) | Situation or goal | What they said | What they did | Need or pain point |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

## 2. Define

### StudentBank problem statement

Students need to understand which account and currency they are using before making a transfer, because a combined balance or unclear destination can make it hard to judge what will happen.

### MAUI mobile banking problem statement

Students need to understand their account overview and get clear answers to basic budgeting questions, while knowing which information an assistant can see and what actions are only examples.

These are provisional statements. Revise them after collecting student feedback.

## 3. Ideate

### Options considered

- Show each account balance with its currency and summarize balances separately by currency.
- Filter transfer destinations to accounts with a matching currency and explain when a transfer cannot be made.
- Give users clear success, error, and waiting feedback for actions.
- Separate accounts and activity into easy-to-scan screens, and label sample balances clearly.
- Give students an assistant for general money questions while keeping account data out of the assistant request.
- Keep the Week 3 task app and Week 4 PokéAPI page available as workshop routes.

### Selected direction

Prioritize clear sample financial amounts and privacy boundaries in the mobile app; prioritize currency-aware balances and transfer choices in the web app.

## 4. Prototype

| User need | Prototype decision | Where it appears |
| --- | --- | --- |
| Review accounts at a glance | Add Home and Accounts tabs with fictional sample balances and recent activity | `MyApp/MainPage.xaml`, `MyApp/AccountsPage.xaml` |
| Ask everyday money questions | Add an assistant chat with suggested prompts and a server-side AI integration | `MyApp/AssistantPage.xaml`, `MyApp/Services/BankingAssistantService.cs`, `WebApplication1/Program.cs` |
| Know what the assistant can access | State that it cannot see accounts or move money; send only the typed question to the model | `MyApp/AssistantPage.xaml`, `WebApplication1/Program.cs` |
| Understand money by currency | Display balances per currency; format account and transaction amounts using the account currency | `WebApplication1/wwwroot/index.html` |
| Avoid an incompatible transfer choice | Offer only a different account with the same currency and explain when none is available | `WebApplication1/wwwroot/index.html` |
| Avoid confusing public dashboard figures | Replace placeholder account/admin counts with a short explanation of the service | `WebApplication1/wwwroot/index.html` |
| Read actions with assistive technology | Announce form status and provide visible keyboard focus | `WebApplication1/wwwroot/index.html` |
| Keep tasks after closing the app | Store tasks in the app's private data directory | `MyApp/Models/TaskItem.cs` |
| Understand API search progress | Show a search indicator and prevent repeated taps while loading | `MyApp/PokemonPage.xaml` |

## 5. Test

Run these tasks with students and record what happened before changing the design:

1. Register, sign in, and open an account in a chosen currency.
2. Open two accounts in the same currency and make a transfer; identify the source and destination before submitting.
3. Switch between Home, Accounts, and Assistant; tell what is sample information and which actions move no money.
4. Ask the assistant a budgeting question, then ask whether it can see account data or make a transfer.
5. Use the retained workshop pages to add a task and search for `pikachu`, `25`, and an invalid Pokémon name.
6. Repeat the tasks using keyboard-only navigation and a screen reader where available.

| Task | Where the user hesitated or made an error | What worked | Change to try | Retest result |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

Do not mark the design as user-tested until this table contains observations from real sessions.

## Course alignment

- **Week 3:** The MAUI app now uses Shell tabs and `Grid`, `VerticalStackLayout`, and `CollectionView`/bindable layouts. The task list and detail routes remain in the project for the workshop.
- **Week 4:** The PokéAPI page, typed `HttpClient`, response models, and observable collection remain available as workshop practice.
- **User-centred design session:** This worksheet is a draft ready to transfer to Miro. Interview evidence and usability-test results remain to be collected.
- **Week 6 proposal, critique and pitch:** See [the Week 6 proposal document](WEEK_6_PROPOSAL_CRITIQUE_PITCH.md) for the slide requirements mapped to this prototype, draft MoSCoW priorities, technical risks, a phased plan, and feedback templates. The user research and peer critique tables remain unfilled until real sessions take place.
