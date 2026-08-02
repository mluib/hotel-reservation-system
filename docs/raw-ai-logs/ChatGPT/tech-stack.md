# HotelReservation API — Technology and Design Overview

## 1. Project Overview

The HotelReservation API is an ASP.NET Core Web API application for managing hotel reservations.

The application provides functionality for:
- Hotels
- Rooms
- Customers
- Reservations
- User authentication
- Authorization and access control

The project follows Clean Architecture principles to achieve:
- Separation of concerns
- Maintainable code structure
- Independent business logic
- Testability
- Secure API design

Project vision (extended scope): build a modern Hotel Reservation System using **ASP.NET Core, Angular, Docker, EF Core, and AI-assisted Development**, treating the AI-assisted development workflow itself as a documented, deliberate part of the project — not just a tool used incidentally.

---

## 2. Technology Stack

### Backend

**ASP.NET Core Web API**
- Building REST API endpoints
- Handling HTTP requests and responses
- Middleware pipeline
- Dependency Injection
- Application configuration

**.NET**
- Built-in dependency injection
- Configuration system
- Middleware pipeline
- Async programming model

### Database and Persistence

**Entity Framework Core** — ORM responsible for:
- Database communication
- Entity mapping
- LINQ queries
- Database migrations
- Change tracking

- Production database: SQL Server
- Testing database: SQLite In-Memory

**Reason:** EF Core provides database abstraction and allows testing database behavior without requiring the production database.

---

## 3. Architecture

### Clean Architecture

```
HotelReservation.Api
        |
        v
HotelReservation.Application
        |
        v
HotelReservation.Domain


HotelReservation.Infrastructure
        |
        v
External systems
(Database, Identity, JWT)
```

The dependency direction is towards the inner layers. The Domain layer has no dependency on external technologies.

---

## 4. Domain Layer

Contains the core business rules of the application: entities, business rules, domain invariants.

Examples: Customer, Reservation.

Does not depend on: ASP.NET Core, Entity Framework Core, Database, Identity, JWT.

**Reason:** Business logic should remain independent from technical details.

---

## 5. Application Layer

Contains application use cases.

Responsibilities:
- Coordinate workflows
- Execute business operations
- Apply application-specific rules
- Communicate with repositories through interfaces

Examples: CreateReservation, GetReservationById

Depends on abstractions:
```csharp
IReservationRepository
ICurrentUserService
IJwtTokenService
```

**Reason:** The application should depend on contracts, not implementations.

---

## 6. Infrastructure Layer

Contains technical implementations:
- Database access
- Entity Framework Core configuration
- Repository implementations
- ASP.NET Core Identity integration
- JWT token generation

Example:
```csharp
// Application
IJwtTokenService

// Infrastructure
JwtTokenService
```

**Reason:** External technologies can be replaced without changing business logic.

---

## 7. API Layer

Contains:
- Controllers
- Middleware configuration
- Swagger configuration
- Dependency Injection setup

Controllers are intentionally kept thin. They should receive requests, call application services, and return responses — not contain business rules or database logic.

---

## 8. Authentication and Authorization

| Feature | Technology |
|---|---|
| User management | ASP.NET Core Identity |
| Authentication | JWT Bearer Authentication |
| Authorization | ASP.NET Core Authorization |
| Access control | Role-based Authorization |

---

## 9. ASP.NET Core Identity

Manages application users: creating users, password hashing/validation, managing roles and user-role relationships.

Identity answers: **"Who are the users?"**

Stores: Users, Roles, User claims.

---

## 10. Authentication

Verifies the identity of a user.

```
User Login
    |
    v
ASP.NET Core Identity validates credentials
    |
    v
AuthService
    |
    v
JwtTokenService creates JWT
    |
    v
Client receives token
```

Authentication answers: **"Who are you?"**

---

## 11. JWT Bearer Authentication

Provides stateless authentication for API requests.

Client sends:
```
Authorization: Bearer <token>
```

API validates: Signature, Expiration, Claims.

JWT contains: User ID, Username, Roles.
JWT does not contain: Password, Password hash.

**Reason:** The API does not need to maintain server-side sessions.

---

## 12. Authorization — ASP.NET Core Role-based Authorization

Controls access to API resources.

```csharp
[Authorize(Roles = "Admin")]
```

Example permissions:
- Customer: Create reservations, View own reservations
- Admin: Manage rooms, Delete reservations, View all reservations

Authorization answers: **"What is this user allowed to do?"**

---

## 13. Authentication Service Design — AuthService

Registration:
```
Register request
    |
    v
Create Identity user
    |
    v
Assign Customer role
    |
    v
Generate JWT
```

Login:
```
Email/password
    |
    v
Identity validates credentials
    |
    v
Generate JWT
```

The AuthService is responsible for authentication, not authorization.

---

## 14. JWT Token Service — IJwtTokenService

JWT generation logic is hidden behind an interface (`IJwtTokenService` / `JwtTokenService`).

**Reason:** The Application layer should not directly depend on JWT implementation details.

---

## 15. Role Design Decision

Normal users receive the Customer role automatically during registration. Users cannot select their own role.

**Reason:** Allowing role selection would create a security vulnerability, e.g.:
```json
{
  "email": "user@test.com",
  "role": "Admin"
}
```

---

## 16. Customer and IdentityUser Relationship

IdentityUser: authentication information, password, roles.
Customer: business entity, reservation ownership.

```
IdentityUser
      |
      | IdentityUserId
      v
Customer
      |
      v
Reservation
```

**Reason:** Authentication data and business data have different responsibilities.

---

## 17. Ownership Security

Customers should only access their own reservations.

```
JWT User ID
      |
      v
CurrentUserService
      |
      v
Application Layer
      |
      v
Ownership validation
```

Security rules:
- Customer: can access own reservations
- Admin: can access all reservations

---

## 18. Swagger JWT Support

Configured to support JWT authentication:
- JWT authorization button
- Sending Bearer tokens
- Testing protected endpoints

Testing flow: Register user → Login → Copy JWT token → Click Swagger Authorize → Enter `Bearer <token>` → Call protected endpoints.

---

## 19. Middleware

```csharp
app.UseAuthentication(); // Validate JWT, create authenticated user
app.UseAuthorization();  // Check permissions, apply authorization rules
```

Execution order:
```
Request → Authentication → Authorization → Controller
```

---

## 20. Testing Strategy

Technologies: xUnit, FluentAssertions, Moq, WebApplicationFactory, SQLite In-Memory.

Tests are separated according to architecture.

### 21. Domain Tests (Unit Tests)

Test business rules only. No database, API, or framework dependencies.

Examples:
- Customer: empty email validation, property updates
- Reservation: invalid date validation, cancellation behavior

### 22. Application Tests (Unit Tests)

Test application use cases. External dependencies mocked using Moq.

Example — CreateReservation: invalid dates rejected, overlapping reservations rejected, successful creation.

### 23. Integration Tests

Test complete application behavior: API endpoints, authentication, authorization, middleware, EF Core integration.

**WebApplicationFactory** creates a real test version of the ASP.NET Core application, using **TestServer** as an in-memory HTTP server.

**SQLite In-Memory Database** used instead of the development database — isolated, fast, no external dependency.

### 24. Current Test Coverage

**Domain Tests:** Customer validation, Customer update, Reservation validation, Reservation cancellation
**Application Tests:** Invalid reservation dates, Reservation overlap detection, Successful reservation creation
**Integration Tests:** User registration, User login, JWT authentication, Protected endpoint access

### 25. Future Improvements

**Authorization:**
- Anonymous user receives 401
- Customer cannot access admin endpoints
- Admin can access admin endpoints

**Ownership:**
- Customer can access own reservation
- Customer cannot access another customer's reservation

---

## Summary

### Technologies
ASP.NET Core Web API, .NET, Entity Framework Core, SQL Server, SQLite In-Memory, ASP.NET Core Identity, JWT Bearer Authentication, ASP.NET Core Role-based Authorization, xUnit, FluentAssertions, Moq, WebApplicationFactory

### Main Design Decisions
- Clean Architecture for separation of concerns
- Domain independent from infrastructure
- Application depends on abstractions
- Identity separated from business entities
- JWT for stateless API authentication
- Role-based authorization for access control
- Automated tests separated by architectural layer

---

## Extended Project Scope (Fullstack + AI-assisted Development)

### Backend (detail)

| Area | Details |
|---|---|
| REST API | ASP.NET Core Web API, OpenAPI, Swagger, CRUD endpoints (e.g. `CustomersController`), DTOs (e.g. `CreateCustomerRequest`) |
| Clean Architecture | Dependencies: API/Infrastructure → Application → Domain. Validation layers: API (required fields), Application (available reservation dates), Domain (valid check-in/check-out). DI via `builder.Services.AddScoped<>`, constructor injection. Repository Pattern (e.g. `IReservationRepository`) |
| Persistence | EF Core (Code First, migrations), SQL Server |
| DDD-inspired domain modeling | Domain layer + use cases in Application layer, entities protect invariants, ubiquitous language, repository abstraction |
| SOLID principles | SRP (Controller vs. Use Case vs. Entity vs. Repository), OCP (`IReservationRepository` allows different implementations), LSP (`IReservationRepository`), ISP (small interfaces: `IReservationRepository`, `IRoomRepository`), DIP (`CreateReservation` depends on `IReservationRepository`) |
| Security | ASP.NET Core Identity, JWT Bearer Authentication, ASP.NET Core Role-based Authorization |
| Testing | xUnit + FluentAssertions; Domain Tests (unit), Application Tests (unit, Moq), API/Infrastructure Tests (integration, WebApplicationFactory + TestServer + SQLite in-memory) |

### Review Backlog

- Consistent REST style
- Proper Swagger response documentation (`ProducesResponseType`)
- DTO review
- DDD improvements (value objects like `EmailAddress`/`Money`, domain services/events, aggregate review)
- API response improvements (status codes)
- Validation improvements: API validation (required, stringlength, emailaddress, ModelState handling, FluentValidation, `ProblemDetails`, global exception handling)
- Review unnecessary database calls
- Repository review (e.g. `Include`s)
- Pagination for larger datasets
- Proper logging
- JWT: role/user seeding, JWT key cleanup
- Tests: Authorization integration tests, Application ownership tests, Repository/infrastructure tests (save/load reservations)

### Frontend

- Angular

### DevOps

- Azure/GitHub DevOps
- Docker
- CI/CD

### AI-assisted Development

- **ChatGPT:** project idea, basic concepts, basic architecture and implementation, generating prompts
- **GitHub Copilot:** ask and agent mode (e.g. create validation logic, create CRUD endpoints)
- **Claude / Claude Code:** architecture discussion, code generation and review, Angular frontend scaffolding, DevOps setup, and maintaining the AI-workflow log documenting agent-driven decisions and corrections
