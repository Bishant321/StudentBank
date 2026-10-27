# User-centred design worksheet

This is a working draft for the technical session. It is based on the StudentBank prototype and the MAUI workshop app. The Miro board was not readable from this workspace, so copy this draft into the board and replace hypotheses with evidence from actual student feedback.

## 1. Empathize

### Intended users

- Students who need a simple view of their banking accounts and recent activity.
- Students who track coursework and personal tasks on a phone.

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

### MAUI task app problem statement

Students need to capture a task, due date, and completion status quickly, and trust that the information remains available after closing the app.

These are provisional statements. Revise them after collecting student feedback.

## 3. Ideate

### Options considered

- Show each account balance with its currency and summarize balances separately by currency.
- Filter transfer destinations to accounts with a matching currency and explain when a transfer cannot be made.
- Give users clear success, error, and waiting feedback for actions.
- Keep task creation short, make due dates and completion states easy to scan, and save tasks locally.
- Show a busy indicator during Pokémon searches and avoid duplicate requests.

### Selected direction

Prioritize clear financial amounts and transfer choices in StudentBank; prioritize quick task entry, persistent task data, and clear search feedback in the MAUI app.

## 4. Prototype

| User need | Prototype decision | Where it appears |
| --- | --- | --- |
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
3. Add a task with a due date, mark it complete, close and reopen the app, and confirm it is still present.
4. Search for `pikachu`, search for `25`, and try an invalid name while observing whether progress and errors are clear.
5. Repeat the tasks using keyboard-only navigation and a screen reader where available.

| Task | Where the user hesitated or made an error | What worked | Change to try | Retest result |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

Do not mark the design as user-tested until this table contains observations from real sessions.

## Course alignment

- **Week 3:** The MAUI project has a page plan, Shell routes, task list, add-task/detail pages, shared styles, and `Grid`, `VerticalStackLayout`, and `CollectionView` layouts.
- **Week 4:** The MAUI project uses PokéAPI through a typed `HttpClient`, response models, and an observable collection displayed in a `CollectionView`.
- **User-centred design session:** This worksheet is a draft ready to transfer to Miro. Interview evidence and usability-test results remain to be collected.
