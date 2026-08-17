# Phase 3 — Angular frontend build

## Context

Phase 1 (UX/design pass) and Phase 2 (backend contract pass) are both finished:
Phase 1 produced the Claude Design mockup at `docs/raw-ai-logs/ClaudeDesignMockup/project/Hotel Reservation System.dc.html` (4 flows: auth, browse/book, my reservations, admin — plus a Home screen added during the redesign). Phase 2 implemented all four flagged gaps from that review (server-side room filtering, reservation price snapshot, customer cancel endpoint, room/hotel photo upload) plus the delete-rejection fix, confirmed present in the current controllers/DTOs and the `WORKFLOW_LOG.md` history.

Per `docs/ROADMAP.md`, Phase 3 is: build the Angular frontend against that mockup and the stabilized API, feeding small contract tweaks back to the backend as they surface. The user has no Angular experience and wants Node.js installed manually (with guidance), and wants Phase 3 built as one continuous pass rather than staged checkpoints.

Two real gaps surfaced while inspecting the backend for this plan (not just cosmetic): there's no CORS policy (the Angular dev server can't call the API at all without it), and there's no way to obtain an Admin account (register always assigns "Customer"; no seeding exists). Both are addressed below as small, scoped backend additions, approved by the user.

## Prerequisite: Node.js (installed — one restart still needed)

Angular's tooling runs on **Node.js** (a JavaScript runtime) via **npm** (the package manager bundled with it). Angular itself is written in **TypeScript** (a typed superset of JavaScript) — `ng new` scaffolds `.ts` files, and the CLI compiles them to JS. The **Angular CLI** (`@angular/cli`) does the scaffolding/build/dev-server work; it doesn't need a separate global install, since `npx @angular/cli new` (used below) downloads it on demand.

Already installed at `C:\Program Files\nodejs` (confirmed: `node` v24.19, `npm` v11.17 when invoked directly, and that folder is already on the **Machine** PATH). Directly invoking the executables works right now; running plain `node`/`npm` from a shell does not yet, because this session's shells inherited their environment before the install happened. Restarting the Claude Code session (not just opening a new terminal window) picks up the refreshed PATH, since the shells this session uses inherit from Claude Code's own process. **Please restart the session once, then confirm `node --version` works before the Angular scaffolding step.**

## Backend tweaks (small, scoped — approved)

All three in `backend/HotelReservation.Api/Program.cs` and one new small use case, mirroring existing patterns:

1. **CORS**: add a named policy allowing the Angular dev origin (`http://localhost:4200`), all headers/methods, before `UseAuthentication()`/`UseAuthorization()`. Origin list read from config so it's easy to extend later (e.g. for Phase 4's Docker setup).
2. **Enums as strings**: `AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` — `RoomType`/`ReservationStatus` serialize as `"Single"`/`"Confirmed"` etc. instead of `0`/`1`. Query-string binding (`RoomFilterRequest.Type`) already accepts enum names independent of this, so no behavior change there.
3. **`GET /api/customers/me`** (`Authorize(Roles="Customer")`): new `GetCurrentCustomer` use case in `Application/Customers/`, same shape as the existing `GetMyReservations` (uses `ICurrentUserService` + `ICustomerRepository.GetByIdentityUserIdAsync`), returns `CustomerDto`. Gives the nav bar the logged-in customer's name without decoding it out of the JWT. Add one unit test mirroring the existing customer tests.
4. **Dev-only Admin seed**: on startup, only when `app.Environment.IsDevelopment()`, ensure the `Admin`/`Customer` Identity roles exist and seed one admin login from `appsettings.Development.json` (e.g. `Seed:AdminEmail` / `Seed:AdminPassword`) if that user doesn't already exist, assigning the Admin role. Explicitly provisional — flagged in code with a comment pointing at the Phase 6 backlog item ("JWT: role/user seeding") for the real design later.

Run `dotnet build` + full test suite after these to confirm nothing regresses. **Then pause: show the diff and ask for a go-ahead to commit before starting the Angular work** — a separate commit for the backend tweaks, distinct from the frontend scaffold/implementation commit(s) that follow.

## Angular scaffold & conventions

- `npx @angular/cli new` run *inside* `frontend/` (the placeholder `README.md` gets replaced by the generated app; its two sentences of content are already captured by the roadmap). Options: standalone components (current default, no NgModules), routing enabled, CSS stylesheets (not SCSS — the mockup's styling is flat color/spacing values, plain CSS custom properties are enough), no SSR (plain client-rendered SPA, matches a .NET API backend and the project's scope).
- **Styling**: global `src/styles.css` defines CSS custom properties for the mockup's palette/fonts (background `#faf8f4`, text `#262420`, accent `#a6522d`, borders `#e6e1d6`/`#d6cfc0`, muted text `#736e64`, confirmed/cancelled status colors, `Plus Jakarta Sans` for headings + `Work Sans` for body via the same Google Fonts link the mockup uses). Each component gets its own scoped `.css` recreating the mockup's actual layout values (grid/flex, spacing, radii) — reading the values directly out of the mockup file rather than reinterpreting the design.
- **HTTP/auth**: functional interceptor (`withInterceptors`) attaches `Authorization: Bearer <token>` from `AuthService`. `AuthService` decodes the JWT payload itself (base64url decode + `JSON.parse`, no library needed) to get role/user id — verified against a real token from `/api/account/register` early in implementation, since the token's role/name claims use the full `ClaimTypes` URIs (`.../identity/claims/role`, `.../name`), not the short `"role"`/`"name"` keys; the decoder checks both. Token stored in `localStorage`; hydrated on app start; cleared on logout/expiry.
- **State**: no NgRx — a small set of injectable services (`AuthService`, `RoomsService`, `ReservationsService`, `CustomersService`, `HotelService`) wrapping `HttpClient`, exposing typed observables/signals. Local UI state (filters, sort, open dialogs) lives in each feature component via Angular signals, same shape as the mockup's local `state` object.
- **Forms**: Reactive Forms throughout (login/signup, room/customer/reservation dialogs, hotel settings, booking dates, filters), with a small custom validator for "check-out after check-in" reused across booking/filter/reservation-edit.

## Folder structure

```
src/app/
  core/
    auth/          auth.service.ts, auth.interceptor.ts, auth.guard.ts, role.guard.ts, jwt.util.ts
    models/        room.model.ts, hotel.model.ts, customer.model.ts, reservation.model.ts
    services/      rooms.service.ts, reservations.service.ts, customers.service.ts, hotel.service.ts
  shared/
    confirm-dialog/, error-dialog/         generic dialogs reused across features
  layout/
    nav/                                   role-aware nav bar (hotel name, links, sign-in/user label)
  features/
    home/
    auth/                                  sign-in / create-account (tabbed, one route)
    rooms/                                 browse + filters, room card
    booking/                               book-a-room page
    reservations/                          "My Reservations"
    admin/
      admin.routes.ts                      child routes below
      rooms-tab/ reservations-tab/ customers-tab/ hotel-tab/
      dialogs/   room-dialog/ customer-dialog/ reservation-dialog/
  app.routes.ts
  app.config.ts
environments/        environment.ts (apiBaseUrl), environment.development.ts
```

## Routing

Real routes replace the mockup's internal `view`/`adminTab` state — deep-linkable and back-button friendly, otherwise equivalent:

| Path | Access | Notes |
|---|---|---|
| `/` | public | Home |
| `/auth` | public | redirects away if already logged in |
| `/rooms` | public | browse + filters (server-side, via `RoomFilterRequest`) |
| `/rooms/:id/book` | Customer | `authGuard` + `roleGuard('Customer')`, redirect to `/auth?returnUrl=...` if not logged in |
| `/reservations/mine` | Customer | same guard |
| `/admin` → `/admin/rooms` | Admin | `roleGuard('Admin')`; child routes `rooms`, `reservations`, `customers`, `hotel` |

## Screen-by-screen mapping (from the mockup)

- **Nav**: hotel name (`HotelService`), Home/Rooms/My Reservations (if Customer)/Admin (if Admin) links, Sign In or user-label+Log out. Drop the mockup's demo public/customer/admin role-switcher — that only existed to fake roles without a backend; the real app derives role from the JWT.
- **Home**: hero text (hotel name/address/description) + "View Rooms" + hero photo or placeholder box (`HotelDto.imageUrl`).
- **Auth**: sign-in/create-account tabs → `AuthService.login/register` → real `/api/account/*`; same inline validation-error banner styling.
- **Rooms**: check-in/out, type, min/max price filters call the real server-side filtered endpoint (debounced), replacing the mockup's client-side filtering; room grid with photo-or-placeholder, price, "View & Book" → routes to booking (guarded).
- **Booking**: room summary, date inputs with live nights×price total and the same inline validation (dates required, checkout>checkin, and the server's overlap rejection surfaced as the same error banner) → on success, route to My Reservations with a "Booking confirmed" flash.
- **My Reservations**: table joining reservations with the already-fetched room list (server returns bare `roomId`/`customerId`, no join — done client-side, same as the mockup did against its mock data) → Cancel (if Confirmed) through the shared confirm dialog.
- **Admin — Rooms tab**: sortable table (client-side sort/toggle, same as mockup), Add/Edit dialog, Delete via confirm dialog → server rejection (room has reservations) shown via the shared error dialog. **Deviation from the mockup**: the photo control in "Add" mode is disabled/hidden, since the real photo-upload endpoint needs a server-issued room id that doesn't exist until after creation — upload only becomes available once editing an existing room.
- **Admin — Reservations tab**: sortable table joining customer name + room number client-side (rooms/customers lists already available to Admin), Edit (dates/status, same overlap re-check), Cancel, Delete, and the "jump to customer/room" links with the same short highlight-fade.
- **Admin — Customers tab**: sortable table, Edit/Delete with the same delete-rejection dialog.
- **Admin — Hotel tab**: name/address form, Save + "Saved" flash + "Unsaved changes" indicator, admin-only photo upload, and a `CanDeactivate` guard reproducing the mockup's three-way "Keep editing / Discard / Save & continue" dialog when navigating away with unsaved changes.
- **Shared dialogs**: `ConfirmDialogComponent`, `ErrorDialogComponent` (generic, message-parameterized), reused everywhere the mockup reuses them.

## Note on the test database

The manual browser-driven verification pass below runs against the actual dev SQL Server database already configured in `appsettings.json` (`localhost` / `HotelReservationDb`) — the same one the backend has used throughout development so far, currently holding a small amount of prior test data (1 room, 4 users, 3 reservations, 4 customers). It's distinct from the SQLite in-memory database the automated integration test suite uses, so the two never interfere. Verifying the frontend will add a handful more real rows to it (a test customer signup, a room or two, a booking) — no separate throwaway environment is being set up for this (that's Phase 4/Docker territory).

## Verification

1. `dotnet build` and full three-tier test suite green after the backend tweaks (before touching the frontend).
2. `ng build` compiles cleanly once the Angular app is scaffolded and implemented.
3. End-to-end manual pass against `ng serve` + the running API, driven from Claude Code's own in-app Browser pane (a browser view inside this session — separate from the user's real Chrome — that Claude Code can navigate and click through directly, so the flows below can be exercised and screenshotted without the user having to drive them manually): register → sign in → browse/filter rooms → book a room → see it in My Reservations → cancel it; sign in with the seeded dev Admin account → exercise all four admin tabs (add/edit/delete room with a reservation-blocked delete, edit/cancel/delete a reservation, edit/delete a customer, edit hotel details + upload a photo + trigger the unsaved-changes dialog).
4. Append a `WORKFLOW_LOG.md` entry once the build lands, per the standing instruction in `CLAUDE.md`.
