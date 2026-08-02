# HotelReservation Project — Overview of Decisions, Goals, and Reasoning

## Main Goal

Build a Hotel Reservation Web API using ASP.NET Core, following Clean Architecture principles, with:

- clear separation of responsibilities
- secure authentication and authorization
- proper domain modeling
- automated testing
- maintainable architecture

The goal is not just to make endpoints work, but to structure the application like a professional production system.

---

## 1. Architecture Direction

### Clean Architecture

The project is separated into:

```
HotelReservation.Domain
        |
        v
HotelReservation.Application
        |
        v
HotelReservation.Infrastructure
        |
        v
HotelReservation.Api
```

### Domain

Contains pure business logic.

Examples:
- Customer entity
- Reservation entity
- business rules
- invariants

Should not know about:
- databases
- EF Core
- ASP.NET Core
- JWT
- Identity

**Reason:** Business rules should be independent from technical details.

### Application

Contains application use cases.

Examples:
- CreateReservation
- GetReservationById
- Authentication contracts
- Interfaces

Responsibilities:
- coordinate workflows
- apply application rules
- call repositories/services

Should not contain:
- EF Core code
- SQL
- HTTP concerns

### Infrastructure

Contains technical implementations.

Examples:
- EF Core DbContext
- repositories
- ASP.NET Core Identity integration
- JWT token generation

**Reason:** Infrastructure can change without affecting business logic.

### API

Contains:
- controllers
- HTTP configuration
- middleware
- Swagger
- dependency injection setup

Controllers should stay thin. They should:
- receive requests
- call application layer
- return responses

They should not contain business logic.

---

## 2. Authentication and Authorization

### Goal

Secure the API using:
- ASP.NET Core Identity
- JWT
- Role-based authorization

### ASP.NET Core Identity

Used for:
- user management
- password hashing
- roles

It manages:
- Users
- Passwords
- Roles
- User-role relationships

Example:
```
IdentityUser
    |
    +-- Customer role
    +-- Admin role
```

Identity is your application's internal identity system.

### Authentication

Question: **"Who are you?"**

Implemented using:
- Login endpoint
- ASP.NET Core Identity password validation
- JWT token generation

Flow:
```
User login
     |
     v
Identity validates credentials
     |
     v
AuthService
     |
     v
JwtTokenService creates token
     |
     v
Client receives JWT
```

### JWT

JWT contains identity information, e.g.:
- UserId
- Username
- Roles

It does NOT contain:
- Password
- Password hash
- Full user data

JWT is only a signed proof of identity.

### Authorization

Question: **"Are you allowed to do this?"**

Implemented using:
```csharp
[Authorize]
[Authorize(Roles="Admin")]
```

Example:
```
Customer:
    create reservation

Admin:
    manage rooms
    delete reservations
    view all reservations
```

Technology: ASP.NET Core Authorization, Role-based Authorization

---

## 3. Authentication Architecture

Created `IJwtTokenService` as an Application abstraction:
```csharp
GenerateToken(...)
```

**Why?** Because Application should not depend on JWT implementation.

Infrastructure provides:
```
IJwtTokenService
        |
        v
JwtTokenService
```

Using an interface allows replacing JWT later.

### AuthService

Purpose: coordinate authentication use cases.

Register:
```
Request
 |
 v
Create Identity user
 |
 v
Assign role
 |
 v
Create JWT
```

Login:
```
Email/password
 |
 v
Identity validation
 |
 v
Create JWT
```

It is mainly an authentication service, not authorization.

---

## 4. User Roles

Decision: normal registration creates `Customer`.

**Reason:** Allowing users to choose roles would be insecure.

Bad:
```
POST /register
{
 role:"Admin"
}
```
Anyone could become admin.

Better:
```
Register
 |
 v
Customer role assigned automatically
```

Admins are created separately.

---

## 5. Customer and IdentityUser Relationship

Important design decision.

The problem: a reservation belongs to a domain `Customer`, but authentication belongs to `IdentityUser`.

Solution — link them:
```
IdentityUser
      |
      | IdentityUserId
      v
Customer
      |
      v
Reservations
```

**Reason:** A logged-in user must map to a business customer.

Example:
```
JWT: UserId = abc123
Find: Customer.IdentityUserId == abc123
Then retrieve reservations.
```

---

## 6. Ownership Security

Requirement: customers should only see their own reservations.

Not:
```
GET /reservations/123   -> exposes anyone's data
```

Implemented concept:
```
Current user from JWT
        |
        v
Application layer
        |
        v
Compare ownership
```

Added `ICurrentUserService` / `CurrentUserService`.

**Why?** Because controllers should not contain security/business rules.

Alternative considered:
- `GET /reservations/{id}` — only for admins
- `GET /reservations/mine` — for customers

This simplifies ownership checks.

---

## 7. Swagger JWT Support

Goal: test secured endpoints directly from Swagger.

Needed:
- **Security Definition** — tells Swagger there is a Bearer token
- **Security Requirement** — tells Swagger to send the token automatically with requests

Both are needed.

Flow:
```
Login
Copy JWT
Swagger → Authorize
Enter: Bearer eyJ...
Call protected endpoints
```

---

## 8. Middleware

Important pipeline:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Order matters.

- Authentication: Who is this user?
- Authorization: Can this user access this?

---

## 9. Testing Strategy

Goal: test each architectural layer correctly.

Using: xUnit, FluentAssertions

### Domain Tests

Purpose: test business rules only. No database, mocks, or API.

Current tests:
- Customer: `Constructor_EmptyEmail_Throws`, `Update_Valid_ChangesProperties`
- Reservation: `Constructor_InvalidDates_Throws`, `Cancel_SetsStatusCancelled`

### Application Tests

Purpose: test use cases. Dependencies are mocked (Moq).

Current tests — CreateReservation:
- `ExecuteAsync_InvalidDates_Throws`
- `ExecuteAsync_Overlapping_Throws`
- `ExecuteAsync_Success_AddsReservation`

### Integration Tests

Purpose: test complete application behavior — API, middleware, JWT, Identity, EF Core.

Using `WebApplicationFactory`, which creates a real test application internally using `TestServer` (fake in-memory web server).

**SQLite In-Memory Database** — used because tests should not touch the development database.

Flow:
```
Integration test
       |
       v
WebApplicationFactory
       |
       v
ASP.NET Core API
       |
       v
SQLite memory database
```

Current integration test: `Register_Login_And_Access_Mine`
Tests: registration, login, JWT generation, authentication middleware, protected endpoint access.

---

## 10. Current Test Coverage

Current state is enough for now.

Next useful tests (priority):

**Authorization tests:**
- No JWT -> 401
- Customer accessing admin endpoint -> 403
- Admin accessing admin endpoint -> success

**Ownership tests:**
- Customer can view own reservation
- Customer cannot view another customer's reservation

---

## 11. Overall Technology Stack

```
ASP.NET Core Web API
Clean Architecture

EF Core
    |
SQL Server (production)
SQLite InMemory (tests)

ASP.NET Core Identity
    |
Users + Roles

JWT Bearer Authentication
    |
Stateless API authentication

ASP.NET Core Authorization
    |
Role-based endpoint protection

xUnit, FluentAssertions, Moq, WebApplicationFactory
```

---

## Current Status

Completed:
- ✅ Clean Architecture structure
- ✅ JWT authentication
- ✅ Identity integration
- ✅ Role-based authorization
- ✅ Swagger JWT support
- ✅ Protected endpoints
- ✅ Ownership concept
- ✅ Initial test suite
- ✅ Integration test infrastructure

Next logical steps:
- Fix/verify integration tests
- Add authorization tests
- Finish Customer ↔ IdentityUser linking
- Continue remaining business features (rooms, reservations, admin workflows)
- Frontend (Angular)
- DevOps (Docker, CI/CD)
