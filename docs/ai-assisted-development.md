# AI-Assisted Development

AI tools assisted throughout: planning, design, code generation, code review, refactoring, test generation, bug-fixing, database migrations, and this documentation — always directed and reviewed, never unsupervised. This file is the short, curated version of how and where; [`workflow-log.md`](workflow-log.md) is the complete dated record of the actual conversations (one bullet per notable interaction, `[developer input] -> [what resulted]`). [`raw-ai-logs/`](raw-ai-logs/) holds original transcripts for *some* of these tools — not a complete archive of every AI session: Claude Code's own conversations aren't exported there, only its saved plans and artifacts.

## Tool attribution

| Tool | Used for |
|---|---|
| **ChatGPT** | Initial project idea, planning, architecture discussion, generating implementation prompts for Copilot |
| **GitHub Copilot** (Visual Studio) | Parts of the initial backend implementation — CRUD, validation, auth, tests — guided and corrected throughout |
| **Claude Design** | The UX/design pass — screen mockups (built on Claude Code's wireframe below) the Angular frontend was built against |
| **Claude Code** | The initial wireframe artifact, architecture/code review, repo restructuring, the entire Angular frontend implementation, DevOps setup, runtime logging, the deep backend review, and this documentation |

## Workflow

Every session follows the same shape: **plan → execute → review → correct.** Nothing gets committed without the developer's explicit go-ahead, regardless of how confident the agent's output looks. Corrections are logged as corrections, not silently absorbed — [`workflow-log.md`](workflow-log.md)'s own bullets show the difference between a developer decision and the agent's own unprompted choice.

## Who did what, by area

- **Backend** — AI-assisted, fully understood. The architecture decisions in [`decisions.md`](decisions.md) are the developer's — genuinely decided, whether the developer raised the question or the AI surfaced it as a choice, never something that shipped without the developer engaging with it. Every generated change is reviewed; wrong or lazy suggestions get rejected or corrected, not accepted because they compiled.
- **DevOps — hands-on by request, not hands-off.** For Docker/docker-compose/CI, the developer asked for a teaching-oriented plan rather than a done-for-you one, made every concrete technical decision, and personally ran, tested, and debugged the real problems that came up — see the first Evidence item below for specifics. Claude Code generated the file content itself, against that plan and those decisions, on request.
- **Frontend** — the one deliberate exception, in the other direction. Claude Code produced both design artifacts (a wireframe, then, via Claude Design, the mockup) and the entire Angular implementation, agentically, end-to-end. But every behavior/flow decision — routing, state approach, what to build vs. explicitly drop — was made by the developer first, reviewing and revising each design step before implementation started. AI wrote the design and the code; it didn't decide what the app should do.

## Evidence, not assertion

A few concrete moments from [`workflow-log.md`](workflow-log.md), chosen because they show *correction*, not just output:

- **Asked for a teaching-oriented plan instead of a done-for-you one** — for the Docker/docker-compose/CI setup, requested exact files/commands plus the conceptual background needed to actually understand each piece, made every concrete technical decision, then personally ran every command and diagnosed the real problems that came up (a Docker Desktop virtualization setting, a missing `ASPNETCORE_ENVIRONMENT` in a verification command, SQL Server networking) rather than asking for a fix and moving on.
- **Rejected a shortcut, asked for the right abstraction instead** — Copilot's first Customer-update implementation used reflection to set an entity id; asked for a proper domain update method instead, since reflection bypasses entity invariants.
- **Caught a real modeling bug before it shipped** — an early pass parsed the JWT user id directly into the customer's own id; recognized and corrected as two genuinely different concepts (auth identity vs. business entity), which is why `Customer.IdentityUserId` exists as a separate link today.
- **Corrected the agent's own drift, more than once** — asked whether the workflow-log's own entries actually held to its stated rules, found a chronology error, a factual reversal, and several bullets phrased as if the developer had asked for something the agent had actually decided on its own; required a full rewrite of the affected entries rather than accepting the first draft.
- **Verified live, not by trusting the report** — repeatedly re-checked claimed fixes against the running app and its real database rather than the agent's say-so (e.g. confirming a new DB constraint actually took effect via the database's own metadata, not just a successful test run).
- **Pushed back on inaccurate framing in this very documentation pass** — corrected an overstated "Copilot did the bulk of the implementation" claim to "parts," and rejected a doc explanation that implied AI-written prose was "in the developer's own words," since that would have contradicted the whole point of documenting this process openly.

## Examples

Picked for technical weight, not routine CRUD or validation asks — prompts that needed real understanding to write, or moments that needed real understanding to catch. The first two quote the actual prompt (frozen ChatGPT archive); the last three are reconstructed from [`workflow-log.md`](workflow-log.md), not verbatim — no Claude Code transcript exists to quote (see the note at the top of this file).

### JWT auth, scoped correctly from the start

- **Prompt** (ChatGPT → Copilot, verbatim, [`raw-ai-logs/chat-gpt/ai-assisted-development.md`](raw-ai-logs/chat-gpt/ai-assisted-development.md)): *"Add authentication to the hotel reservation API. Use ASP.NET Core Identity for user management. Add JWT Bearer authentication. [...] Follow the existing Clean Architecture structure. **Keep authentication concerns outside the Domain layer.**"*
- **Result:** Identity/JWT wiring landed entirely in Infrastructure/Api, never touching Domain.
- **Why it mattered:** the last line is doing the real work — knowing in advance that auth is an *infrastructure* concern, not a domain one, and saying so up front, rather than discovering the leak after the fact and unwinding it.

### Catching an anti-pattern in the result, not just the prompt

- **Prompt** (ChatGPT → Copilot, verbatim, same source): *"Implement Customer CRUD following the existing Clean Architecture pattern in this project. [...] Keep dependency direction unchanged."*
- **Result (first pass):** compiled and worked, but used reflection to set the entity's id on update — silently bypassing the entity's own invariant-protecting constructor.
- **Reviewed:** recognized *why* that mattered, not just that it looked unusual — reflection defeats the whole point of encapsulating state behind private setters. Rejected; replaced with a proper domain `Update()` method instead.

### Removing a spoofable client-supplied id

- **Prompt** (Claude Code, reconstructed): reviewing an early reservation-creation flow, pointed out that accepting a `customerId` field in the request body let any customer create a reservation under another customer's id.
- **Result:** the field was removed entirely.
- **Reviewed:** the customer is now always resolved from the JWT's own subject claim instead of a client-supplied value — closing a real spoofing risk, not a style preference.

### Choosing not to add a pattern

- **Prompt** (Claude Code, reconstructed): scoping the DDD value-object work for Phase 6, asked how far to take it.
- **Result:** built all three planned value objects (`EmailAddress`, `Money`, `DateRange`) — but explicitly ruled domain events out of that same phase.
- **Reviewed:** nothing in the app actually needs domain events yet; knowing when a well-known pattern doesn't earn its cost is as much a decision as knowing when it does. Recorded as a deliberate scope call in `decisions.md`, not an oversight.

### Skipping a backlog item after checking the real numbers

- **Prompt** (Claude Code, reconstructed): before implementing pagination — a line item in the project's own backlog — asked whether it was actually needed.
- **Result:** checked against the real dataset (a handful of rooms and reservations) and the real cost (a coordinated frontend change), then skipped it.
- **Reviewed:** documented as a deliberate non-goal in both `roadmap.md` and `decisions.md`, rather than silently dropped or left looking forgotten.

## Full record

- [`workflow-log.md`](workflow-log.md) — the complete, dated, rule-governed log.
- [`raw-ai-logs/`](raw-ai-logs/) — frozen original transcripts (ChatGPT, GitHub Copilot, Claude Design, Claude Code plans/artifacts) this log was reconstructed from.
