# Backend hardening menu — six points from the external cross-check review

## Context

An external session cross-checked the finished repo (cloned it, read the code and docs, ran the live stack, tested auth/authorization behavior) and produced a written assessment. It flagged six concrete, fixable gaps alongside a lot of praise for architecture/process. The developer confirmed all six are worth planning for, but wants each one laid out as problem + alternatives + a recommendation + an effort estimate first, so the choice of what to actually implement (all, some, or none) is made deliberately afterward — not as a follow-up task, but as the deliverable of this turn.

This plan is that menu. Nothing gets implemented yet. All findings below were verified directly against the current code (not taken on the reviewer's word) via two research passes over `backend/`.

**Decision status:** all six decided — 1A, 2A, 3A, 4A, 5A, 6A. Ready to implement.

---

## 1. Double-booking race condition

**Problem.** `CreateReservation.ExecuteAsync` (`backend/HotelReservation.Application/Reservations/CreateReservation.cs:62-83`) calls `HasOverlappingReservationAsync` — a plain `SELECT`, no locking hint, no transaction — then inserts via `AddAsync`/`SaveChangesAsync`. Nothing spans the check and the insert atomically: no DB constraint, no elevated isolation level, no app-level lock. Two concurrent requests for the same room/overlapping dates can both pass the check and both insert. Confirmed: no `RowVersion`/concurrency token on `Reservation`, no existing transaction pattern anywhere in the codebase (every repository is a bare `SaveChangesAsync()`) — whatever fix is chosen is a first-of-its-kind pattern here, not a "follow the existing convention" change.

**Alternatives:**
- **A — Serializable transaction.** Wrap the check + insert in `BeginTransactionAsync(IsolationLevel.Serializable)`. Idiomatic EF Core, no raw SQL, directly expresses "these two reads/writes must be atomic." Trade-off: SQL Server serializable isolation uses range locks that can deadlock under real concurrent load, which would normally call for retry logic — acceptable to skip for a portfolio-scale demo, but worth being honest that production hardening would want it.
- **B — Per-room advisory lock (`sp_getapplock`).** Acquire an exclusive lock keyed on `RoomId` via raw SQL inside a transaction, do the check + insert, release on commit. Serializes only genuinely conflicting requests (other rooms are unaffected). Trade-off: introduces raw SQL via `ExecuteSqlRawAsync`, ties the fix to SQL Server specifically (already the case for this project, so low real cost).
- **C — DB-level overlap constraint.** SQL Server has no native range/exclusion constraint (unlike Postgres's `EXCLUDE USING gist`); getting an equivalent guarantee would mean normalizing to one row per booked night plus a unique index — a real schema/data-model change. Strongest guarantee, but disproportionate effort for this project's scale.
- **D — Optimistic concurrency (RowVersion + retry).** Add a concurrency token to `Room`, rely on EF's conflict detection to force a retry when two requests touch the same room concurrently. Doesn't directly model the actual invariant (date-range overlap, not "any change to the room"); more indirect and fiddly than A/B for the same guarantee.

**Recommendation:** **A (serializable transaction)** as the primary fix — most idiomatic, smallest conceptual diff, and matches "concurrency correctness is the whole point" for this domain. Pair it with an integration test that fires two concurrent create-requests for the same room/dates and asserts exactly one succeeds (201) and one is rejected (409).

**Complication worth flagging up front:** `Tests.Integration` runs against **SQLite in-memory**, not SQL Server (per `CLAUDE.md`). SQLite's locking/isolation model differs meaningfully from SQL Server's serializable behavior, so a concurrency test written against SQLite may not actually exercise the same guarantee the production SQL Server fix relies on. This needs a decision at implementation time: accept a SQLite-only test that proves *some* serialization occurs (weaker but still real coverage), or run that one test against a real SQL Server test instance instead. Either way, this is the main source of estimate risk below.

**Effort: Medium–High (~2–4 hours),** most of it in the concurrency test, not the fix itself.

**Developer decision (2026-08-26):** Go with **A (serializable transaction)**. The SQLite-vs-SQL-Server testing complication above is accepted as-is — the concurrency test runs against the existing SQLite-backed `Tests.Integration` host, proving serialization occurs, without spinning up a separate real-SQL-Server test environment just for this one test. Deadlock retry logic is deliberately **not** added — under real concurrent load, SQL Server serializable isolation can pick a transaction as a deadlock victim and abort it outright rather than blocking it, and this implementation will let that surface as a failure rather than retrying automatically. This is being left in as a documented, deliberate omission (a code comment at the transaction site, plus a `decisions.md` entry noting retry-on-deadlock was considered and skipped as disproportionate for this project's scale/traffic), not an oversight.

---

## 2. Registration leaks account existence (409 on duplicate email)

**Problem.** `AuthService.LoginAsync` deliberately returns the identical `"Invalid credentials."` for both unknown-email and wrong-password (now commented as intentional). `RegisterAsync` doesn't follow the same discipline: a duplicate email throws `ConflictException` with Identity's own error text, and the global exception middleware returns `ex.Message` verbatim in the response body for every taxonomy exception by design (confirmed at `ExceptionHandlingMiddleware.cs:71` — only unclassified 500s get `Detail = null`). So registration confirms "this email already has an account," while login is careful not to.

**Structural difference from login, worth being precise about:** in `RegisterAsync`, `_userManager.CreateAsync`'s failures split into exactly two buckets — `DuplicateUserName`/`DuplicateEmail` → `ConflictException` (409), everything else (weak password, malformed email) → `ValidationException` (400). Since `UserName == request.Email` in this app, the 409 path is single-cause by construction: it only ever means "this email is taken." That's different from login, where one message deliberately covers two genuinely different causes. Here, the enumeration signal isn't really in the message text at all — **the 409-vs-400 status code already tells a caller whether it's a duplicate-email conflict**, independent of wording.

**Alternatives:**
- **A — Document as an accepted trade-off, no behavior change.** Distinct "you already have an account" feedback on signup is near-universal UX practice; hiding it would make the form actively worse for the common case (typo'd email, forgotten prior signup) for a security benefit that's marginal on a project with no real user base to protect.
- **B — Make registration genuinely symmetric with login.** Because the status code itself is the signal (see above), true symmetry can't stop at rewording the 409's message — it requires collapsing `ConflictException` and `ValidationException` into one status code and one generic message. That means a user submitting a weak password gets the exact same undifferentiated "registration failed" as a user whose email is already taken — no signal telling them *which* to fix. This is a materially worse regression than login's collapse: login's two hidden causes ("no such account" vs "wrong password") lead to the same corrective action either way (retry credentials, or go register); registration's two causes lead to genuinely different corrective actions ("log in instead" vs "pick a different password"), so collapsing them removes information the user needs to complete the form at all, not just a security-irrelevant detail.
- **C — Middle ground.** Keep the distinct 409 (preserve the UX and status-code value) but replace Identity's raw error text with a generic own-written message ("An account with this email already exists."), so at least the *wording* isn't Identity-internal. Doesn't change the enumeration property (the 409 already reveals that), just the message provenance.

**Recommendation: A** — write a `decisions.md` entry (matching the existing `Decision` / `Reason` / `Rejected` format) naming this as a deliberate, considered trade-off, contrasted explicitly against the login behavior so it reads as "decided," not "missed," and noting B was considered and rejected specifically because it would break the legitimate weak-password case, not just because it's "a UX regression" in the abstract. This directly answers the reviewer's actual complaint (inconsistency vs. actual risk) without touching working code.

**Effort: Low (~15–20 min).**

**Developer decision (2026-08-26):** Go with **A** — document only, no behavior change. B rejected as a real functional regression (removes the weak-password signal), C rejected as not actually improving anything (doesn't change the enumeration property, only reword text that isn't the actual signal).

---

## 3. No login rate limiting / lockout

**Problem.** `AddIdentity<IdentityUser, IdentityRole>()` is called with no options lambda at all, and `AuthService.LoginAsync` calls `UserManager.CheckPasswordAsync` directly rather than going through `SignInManager` — confirmed `SignInManager` isn't registered or used anywhere in the codebase. `UserManager.CheckPasswordAsync` only verifies the hash; it never checks `IsLockedOutAsync`, never increments `AccessFailedCount`. So Identity's lockout store exists but is never engaged — login is unlimited-attempt brute-forceable today, despite `AddIdentity`'s defaults looking like lockout is configured.

**Alternatives:**
- **A — Switch to `SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`.** The framework's own supported path for exactly this. Requires injecting `SignInManager<IdentityUser>` into `AuthService` and handling its `SignInResult` (Succeeded / IsLockedOut / failed), plus explicitly setting `options.Lockout.MaxFailedAccessAttempts` / `DefaultLockoutTimeSpan` in `AddIdentity()` instead of leaving them implicit.
- **B — Hand-roll it around the existing `CheckPasswordAsync` call** (`IsLockedOutAsync` before, `AccessFailedAsync`/`ResetAccessFailedCountAsync` around). Smaller diff, but re-implements logic `SignInManager` already gets right (e.g. not counting failures against an already-locked-out account).

**Recommendation: A** — less code, uses the tested framework path. Keep the failure response as the same generic `"Invalid credentials."` even once locked out (not "account locked"), continuing the existing enumeration-avoidance rationale from point 2's contrast — a distinct "locked out" message would let an attacker confirm the account exists once they've thrown enough attempts at it.

**Effort: Low–Medium (~45–60 min),** including extending `AuthenticationIntegrationTests.cs` with a lockout case (needs a short lockout window configured for the test, or asserting `IsLockedOutAsync` directly via the test's DI scope rather than waiting out a real timer).

**Developer decision (2026-08-26):** Go with **A** — inject `SignInManager` and use `CheckPasswordSignInAsync`, less code and less chance of re-deriving Identity's own lockout sequencing incorrectly.

---

## 4. No security headers, `Server: Kestrel` header left in

**Problem.** Confirmed: no `UseHsts()`, no custom header middleware (only `ExceptionHandlingMiddleware`, which is exception-handling only), no rate-limiting middleware, and no `ConfigureKestrel`/`AddServerHeader` anywhere — Kestrel runs on defaults, including sending the `Server: Kestrel` response header.

**Alternatives:**
- **A — Hand-rolled middleware.** A few lines in `Program.cs` (same style as the existing `IsDevelopment()`-gated Swagger block) setting `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, `X-Frame-Options: DENY`, `UseHsts()` outside Development; plus `builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false)`. Zero new dependencies.
- **B — A security-headers NuGet package** (e.g. `NetEscapades.AspNetCore.SecurityHeaders`) for more thorough coverage (nonce-based CSP, etc.). A new dependency for what's fundamentally a handful of static header assignments — inconsistent with this project's otherwise minimal-dependency posture (e.g. DataAnnotations over FluentValidation, no DDD ceremony beyond what's earned its keep).

**Recommendation: A**, and deliberately **skip a real Content-Security-Policy** for now — getting CSP right requires enumerating the Angular build's actual script/style/font origins, and the payoff is low for a same-origin, no-third-party-script SPA behind nginx. Worth one line in `decisions.md` calling that a deliberate non-goal, same treatment as pagination.

**Effort: Low (~20–30 min),** verified with a quick header check against the already-running Docker stack — no test-suite changes needed (not business logic).

**Developer decision (2026-08-26):** Go with **A** — hand-rolled middleware plus the Kestrel `AddServerHeader = false` config, no new dependency. CSP remains a deliberate non-goal per the recommendation above.

---

## 5. Image upload trusts client-supplied Content-Type (no magic-byte check)

**Problem.** `ImageValidation.Validate` (`backend/HotelReservation.Application/Common/ImageValidation.cs`) checks only the client-supplied `Content-Type` header (trivially spoofable) and file size — never the actual bytes. Confirmed the upload pipeline never buffers or re-reads the stream (`ImageStorageService.SaveAsync` does one `CopyToAsync` straight to disk), and no image-processing/file-sniffing package exists in any `backend/` `.csproj`. Real-world impact is low here specifically — filenames are server-generated GUIDs, files are served purely statically, no executable extension is reachable — but it's a known anti-pattern worth closing since a security-conscious pass would normally flag it.

**Alternatives:**
- **A — Hand-rolled magic-byte check.** Buffer the upload (already capped at 5 MB, so a full in-memory buffer is cheap) into a seekable `MemoryStream`, check its first bytes against known JPEG/PNG/WebP signatures, reject on mismatch, then hand that same seekable stream to `SaveAsync`. No new dependency.
- **B — A file-type-sniffing package** (e.g. `MimeDetective`). Broader format coverage, but a new dependency for 3 known, stable, simple signatures that are trivial to hand-check.
- **C — Full re-encode via `SixLabors.ImageSharp`.** Strongest guarantee (a non-image file simply fails to decode; also strips embedded metadata), but the heaviest option — a real dependency, real CPU cost, and changes the stored file's fidelity. Disproportionate to the actual risk profile here.

**Recommendation: A** — hand-rolled, no dependency, matches this project's minimal-dependency conventions. Document in `decisions.md` that B and C were considered and rejected as disproportionate to the actual risk, in the same "considered and rejected, not just absent" style already used elsewhere.

**Effort: Low–Medium (~45–60 min):** the buffering + signature check itself, updating both `UploadRoomImage.cs`/`UploadHotelImage.cs` call sites, and extending the existing `UploadRoomImageTests.cs`/`UploadHotelImageTests.cs` (which already use `MemoryStream` test doubles) with a "spoofed content-type, wrong actual bytes" case.

**Developer decision (2026-08-26):** Go with **A** — hand-rolled magic-byte check, no new dependency.

---

## 6. JWT-in-localStorage / no refresh-token — undocumented trade-off

**Problem.** The frontend stores the JWT in `localStorage` (`auth.service.ts`) with no refresh-token or server-side revocation — a stolen token (e.g. via a future XSS bug) stays valid until natural expiry, and logout only discards it client-side. Every other comparable scope-cut in this project (FluentValidation, pagination, DDD ceremony) has a `decisions.md` entry recording it as deliberate; this one doesn't — the reviewer's single sharpest observation, since it makes the omission read as "missed" rather than "decided," inconsistent with everything else in the docs.

**Alternatives** (this point is a documentation task, not a code change — "alternatives" here means what's being weighed, not competing implementations to build now):
- **A — Document only, no code change.** Add a `decisions.md` entry naming the risk (XSS → token theft, no revocation, no refresh flow) and why it's accepted for this project's scope — same treatment as every other cut.
- **B — Actually change the auth model now** (httpOnly cookie + CSRF, or a refresh-token/revocation store). A materially bigger scope change touching CORS/CSRF handling, the Angular interceptor, and the API's auth pipeline — not a hardening tweak but a design change, late against an already-"done" roadmap, for something both the reviewer and this project's own conventions already treat as standard, acceptable SPA practice.

**Recommendation: A** — the cheapest, highest-signal item on this whole list: it directly answers the reviewer's most specific complaint for near-zero effort. Before writing the entry, confirm the actual configured JWT lifetime (check the `Jwt` section in `appsettings.json` / `JwtTokenService`) so the trade-off states the real exposure window rather than an assumed one.

**Effort: Low (~15–20 min).**

**Developer decision (2026-08-26):** Go with **A** — document only, no code change. Consistent with keeping this round of work scoped to the backend rather than reopening the frontend's auth model (B would require Angular interceptor/CORS/CSRF changes, out of scope here).

---

## What happens next

All six decisions are locked in (1A, 2A, 3A, 4A, 5A, 6A); no code or docs have been touched yet — that's the implementation step that follows this plan's approval. Each item is independent and can be implemented in any order — points 2 and 6 are pure `decisions.md` additions, 3 and 4 are small `Program.cs`/service changes, 5 is a contained Application-layer change, and 1 is the one genuinely multi-step item (fix + concurrency test against the existing SQLite-backed `Tests.Integration` host, deadlock retry deliberately skipped and documented as such). Total effort across all six: roughly 4.5–7 hours, dominated by point 1.

**Verification per item, once selected:**
- 1, 3, 5: extend/run the relevant test project (`dotnet test` on `Tests.Integration`/`Tests.Application`) plus a live check against the already-working Docker Compose stack.
- 2, 6: no runtime verification needed — just confirm the new `decisions.md` entries read consistently with the existing ones.
- 4: quick `curl -I` (or browser devtools) against the Dockerized frontend/backend to confirm the new headers are present and `Server: Kestrel` is gone.

---

## Post-approval note

Everything above was implemented and verified in the session that produced this plan: all 72 backend tests pass (`Tests.Domain`, `Tests.Application`, `Tests.Integration`, including a new `ReservationConcurrencyIntegrationTests` for point 1), and the changes were committed and pushed. See [`docs/decisions.md`](../../decisions.md) and [`docs/workflow-log.md`](../../workflow-log.md) (2026-08-26 entry) for the final record. `ImageValidation.Validate`/`ValidateSignatureAsync` were subsequently merged into a single `ValidateAsync` method per a follow-up developer request.
