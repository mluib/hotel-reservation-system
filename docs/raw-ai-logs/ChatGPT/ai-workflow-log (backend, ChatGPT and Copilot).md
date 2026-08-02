# AI-assisted Development — Workflow Log

Running log of notable AI-agent interactions during development: what was generated, what was wrong or needed correction, and why.

Kept lightweight — a couple of bullet points per entry, added as things happen. Used as raw material for a polished writeup at the end of the project.

Format:

- [what was asked] -> [what the agent produced] -> [correction/decision made, and why]

---

# Log

## Project Planning

- Discussed project goals and possible approaches with ChatGPT -> explored ideas for building a hotel reservation system -> decided to implement an ASP.NET Core Web API using Clean Architecture principles.

- Asked ChatGPT about suitable architecture -> discussed separation of business logic, application logic, infrastructure, and API concerns -> decided to use separate projects:
  - HotelReservation.Domain
  - HotelReservation.Application
  - HotelReservation.Infrastructure
  - HotelReservation.Api

---

# Authentication and Authorization Planning

- Asked ChatGPT about user roles -> discussed whether one user should have multiple roles -> decided that normal users only need the Customer role and administrators are managed separately.

- Asked whether registration should accept a role -> explained that users choosing their own role would create a security problem -> decided registration always assigns the Customer role.

- Asked whether APIs need to receive a role because users are customers -> clarified that role assignment belongs to authentication/identity management, not API input.

---

# Authorization Design

- Asked ChatGPT to define authorization rules for API endpoints -> created authorization plan:
  - Reservations:
    - Customers can create reservations.
    - Admins can view all reservations.
    - Customers can view their own reservations.
    - Admins can delete reservations.
  - Rooms:
    - GET endpoints are public.
    - POST, PUT, DELETE are admin-only.
  - Customers:
    - Admin-only access.

- Asked whether authorization attributes should be placed on controllers or methods -> discussed controller-level and action-level authorization -> decided to use controller-level authorization for defaults and method-level attributes for exceptions.

- Asked about using both Authorize and AllowAnonymous -> clarified that AllowAnonymous overrides authorization requirements -> decided to explicitly allow anonymous access for login and registration endpoints.

---

# Identity Design

- Asked whether ASP.NET Core Identity is an authentication service -> clarified that Identity manages users, passwords, roles, and identity data, while authorization is handled by ASP.NET Core Authorization.

- Asked about authentication versus authorization -> clarified:
  - Authentication answers: "Who are you?"
  - Authorization answers: "What are you allowed to do?"

- Discussed separation between IdentityUser and domain Customer -> identified that authentication users and business customers represent different concepts -> decided to link them using IdentityUserId.

---

# JWT Design

- Asked whether UserId from JWT is a Guid -> clarified that JWT claims are strings and can be converted to Guid when needed.

- Asked whether mapping JWT UserId to Customer IdentityUserId would be problematic -> confirmed that consistent storage and conversion solves the issue.

- Defined JWT authentication flow:
  - ASP.NET Core Identity validates credentials.
  - AuthService handles authentication workflow.
  - JwtTokenService creates tokens.
  - ASP.NET Core middleware validates tokens.

---

# Copilot Request: Protect API Endpoints

- Asked Copilot to implement authorization for API endpoints -> Copilot added Authorize attributes to controllers and actions.

Implemented:

- Reservation authorization.
- Room management authorization.
- Customer controller restrictions.

Decision:
Accepted the general implementation because endpoint permissions belong in the API layer.

---

# Copilot Request: Implement Ownership Authorization

- Asked Copilot to implement reservation ownership checks -> Copilot added:

  - ICurrentUserService
  - CurrentUserService using IHttpContextAccessor
  - ForbiddenException
  - Ownership validation in application services

Result:

- Admins can access all reservations.
- Customers can access only their own reservations.

Decision:
Accepted because authorization rules affecting business data belong in the Application layer rather than controllers.

---

# Customer and Identity Linking

- Discussed customers existing independently from Identity users -> identified that reservations need a reliable link between authenticated users and domain customers.

Decision:

During registration:

1. Create IdentityUser.
2. Assign Customer role.
3. Create domain Customer.
4. Store IdentityUserId on Customer.

Reason:

A logged-in user must map to a domain customer.

---

# Copilot Request: Automatic Customer Creation

- Asked Copilot to create a Customer automatically during registration -> Copilot implemented Identity user creation and Customer linking.

Decision:
Keep this approach because it avoids customers existing without authentication accounts.

---

# API Design Decisions

- Discussed whether to remove Create Customer API -> identified that registration already creates customers -> decided customer creation should probably happen through registration.

Reason:

Avoid duplicate flows and ensure every customer has an identity account.

---

# Swagger JWT Setup

- Asked Copilot to configure Swagger JWT authentication -> Copilot added OpenApiSecurityScheme configuration.

- Asked whether OpenApiSecurityScheme alone is enough -> clarified that both are required:
  - OpenApiSecurityDefinition defines the authentication method.
  - OpenApiSecurityRequirement applies it to API calls.

Decision:
Use both for Swagger JWT support.

---

# Testing Strategy

- Asked ChatGPT what test layers are needed -> defined:

## Domain Tests

Purpose:

- Test business rules.
- Test entity behavior.

No:

- Database.
- API.
- Framework dependencies.

---

## Application Tests

Purpose:

- Test use cases.
- Verify application logic.

Dependencies:

- Mock repositories and services using Moq.

---

## Integration Tests

Purpose:

- Test the complete application.

Includes:

- API endpoints.
- Authentication.
- Authorization.
- Middleware.
- Database access.

---

# Copilot Request: Create Test Projects

- Asked Copilot to create a test structure -> Copilot created:

  - HotelReservation.Tests.Domain
  - HotelReservation.Tests.Application
  - HotelReservation.Tests.Integration

Added:

- xUnit.
- FluentAssertions.
- Moq.
- WebApplicationFactory.

Decision:
Accepted because the test projects follow Clean Architecture boundaries.

---

# Copilot Request: Integration Test Database Setup

- Asked Copilot to improve integration tests -> Copilot added CustomWebApplicationFactory.

Implemented:

- SQLite In-Memory database.
- Replacement of production database.
- TestServer usage.
- Isolated test database.

Decision:
Accepted because integration tests should never use the developer database.

---

# Integration Test Error

- Running integration tests caused:

"Only a single database provider can be registered"

Problem:

SQL Server and SQLite providers were registered together.

Decision:

Modify test factory so production database registration is replaced correctly before adding SQLite.

---

# Current Test Coverage

Implemented tests:

## Domain

- Customer constructor validation.
- Customer update behavior.
- Reservation date validation.
- Reservation cancellation.

## Application

- Invalid reservation dates.
- Overlapping reservation detection.
- Successful reservation creation.

## Integration

- Register user.
- Login.
- Access protected endpoint.

Decision:

Coverage is sufficient for the current development stage.

Future tests:

- Anonymous access returns 401.
- Forbidden role access returns 403.
- Customer ownership restrictions.

---

# Documentation Requests

- Asked ChatGPT to create project documentation -> generated summaries of:
  - Technologies used.
  - Architecture.
  - Authentication.
  - Authorization.
  - Testing strategy.

- Asked ChatGPT to create architecture decision documentation -> documented:
  - Clean Architecture.
  - Domain rules.
  - DTO usage.
  - Repository pattern.
  - Identity separation.
  - JWT authentication.
  - Authorization.
  - Testing decisions.

---

# AI-Assisted Development Approach

## Decision

AI tools were used as development assistants.

Used for:

- Architecture discussions.
- Code generation.
- Refactoring suggestions.
- Test generation.
- Documentation.

## Human Responsibilities

The developer remains responsible for:

- Architecture decisions.
- Security decisions.
- Reviewing generated code.
- Running tests.
- Final implementation choices.

AI-generated code is reviewed before being committed.