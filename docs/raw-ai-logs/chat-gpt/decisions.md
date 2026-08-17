# Architecture Decisions

This document records important design decisions made during the development of the Hotel Reservation System.

The purpose is to document what decisions were made and why they were chosen.

---

# Clean Architecture

## Decision

The application is structured using Clean Architecture with separate projects:

```
HotelReservation.Api
HotelReservation.Application
HotelReservation.Domain
HotelReservation.Infrastructure
```

## Reason

The goal is to separate business rules from technical details.

Benefits:

- Clear separation of responsibilities
- Easier testing
- Maintainable code structure
- Independent business logic
- Easier replacement of external technologies

## Dependency Direction

Dependencies point inward:

```
API
 |
 v
Application
 |
 v
Domain


Infrastructure
 |
 v
Application
```

The Domain layer has no dependency on other layers or frameworks.

---

# Domain Entities Own Business Rules

## Decision

Business rules are implemented inside domain entities.

Examples:

- Reservation dates must be valid
- Customer data must satisfy business rules
- Reservation status changes must follow business rules

## Reason

Business rules should not depend on:

- Controllers
- Databases
- Frameworks
- External services

The domain model protects its own consistency.

---

# Domain Update Methods

## Decision

Entities provide explicit update methods instead of replacing objects directly.

Example:

```csharp
customer.Update(firstName, lastName, email);
```

## Reason

This keeps validation and business rules inside the domain model.

Reflection-based updates were avoided because they bypass encapsulation.

The entity controls how its state changes.

---

# Use Cases in Application Layer

## Decision

Controllers call application services and use cases.

Example:

```
CustomersController
        |
        v
CreateCustomer
        |
        v
ICustomerRepository
```

## Reason

Controllers should only handle HTTP concerns.

Business workflows belong in the Application layer.

Benefits:

- Thin controllers
- Better testability
- Reusable application logic

---

# DTOs Between API and Application

## Decision

The API does not expose domain entities directly.

DTOs are used for communication.

Examples:

- CreateCustomerRequest
- UpdateCustomerRequest
- CustomerDto
- ReservationDto

## Reason

Benefits:

- API contracts are independent from domain models
- Prevents accidental data exposure
- Allows API changes without changing domain entities
- Avoids exposing internal implementation details

---

# Repository Pattern

## Decision

The Application layer depends on repository interfaces.

Example:

```
Application

IReservationRepository
        ^
        |
Infrastructure

ReservationRepository
```

## Reason

The Application layer should not depend directly on Entity Framework Core.

Benefits:

- Better testability
- Separation of concerns
- Flexible data access implementation
- Easier mocking in unit tests

---

# Entity Framework Core and SQL Server

## Decision

Entity Framework Core is used for database access.

Production database:

- SQL Server

Testing database:

- SQLite In-Memory

## Reason

Benefits:

- Native .NET integration
- Database migrations
- Strong typing
- LINQ support
- Reduced database boilerplate

---

# REST API Design

## Decision

API endpoints follow REST conventions.

Example:

```
GET    /api/customers
POST   /api/customers

GET    /api/customers/{id}
PUT    /api/customers/{id}
DELETE /api/customers/{id}
```

## Reason

HTTP methods represent operations.

| Operation | HTTP Method |
|---|---|
| Create | POST |
| Read | GET |
| Update | PUT |
| Delete | DELETE |

Benefits:

- Predictable API design
- Standard client interaction
- Easier API usage

---

# Validation Separation

## Decision

Validation responsibilities are separated by layer.

## Domain Layer

Responsible for:

- Entity rules
- Valid object state
- Business invariants

Example:

```
Reservation end date must be after start date
```

## Application Layer

Responsible for:

- Use case validation
- External state checks

Examples:

```
Does the customer exist?

Is the room available?

Does a reservation conflict exist?
```

## Infrastructure Layer

Responsible for:

- Database access
- Queries
- Persistence

Example:

```
How do I retrieve reservations?
```

---

# ASP.NET Core Identity

## Decision

ASP.NET Core Identity is used for user and role management.

Responsibilities:

- User creation
- Password hashing
- Password validation
- Role management
- User-role relationships

## Reason

Authentication data and business entities have different responsibilities.

Identity manages:

```
Who is the user?
```

The domain manages:

```
What is a customer?
```

---

# Customer and IdentityUser Separation

## Decision

Identity users and domain customers are separate entities.

Relationship:

```
IdentityUser

      |
      |
IdentityUserId

      |
      v

Customer

      |
      v

Reservation
```

## Reason

IdentityUser represents authentication concerns:

- Login credentials
- Password
- Roles

Customer represents business data:

- Customer information
- Reservations

This keeps authentication concerns out of the domain model.

---

# JWT Authentication

## Decision

JWT Bearer Authentication is used for API authentication.

## Reason

JWT provides stateless authentication.

The server does not need to store user sessions.

Flow:

```
User Login

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

Client sends token with requests
```

---

# Authentication Service

## Decision

Authentication workflows are handled by an AuthService.

Responsibilities:

- Register users
- Login users
- Assign default roles
- Generate authentication responses

## Reason

Authentication workflow should not be implemented inside controllers.

Controllers remain focused on HTTP communication.

---

# JWT Token Service Abstraction

## Decision

JWT creation is hidden behind an interface.

Example:

```csharp
IJwtTokenService
```

Implementation:

```csharp
JwtTokenService
```

## Reason

The Application layer should not depend directly on JWT implementation details.

Benefits:

- Easier testing
- Lower coupling
- Replaceable authentication implementation

---

# Role-Based Authorization

## Decision

ASP.NET Core Role-based Authorization is used to protect API endpoints.

Example:

```csharp
[Authorize(Roles = "Admin")]
```

## Reason

Different users have different permissions.

Example:

Customer:

- Create reservations
- View own reservations

Admin:

- Manage rooms
- Delete reservations
- View all reservations

Authorization answers:

```
What is this user allowed to do?
```

---

# User Role Assignment

## Decision

Normal registration automatically assigns the Customer role.

## Reason

Users should not be able to select their own privileges.

Incorrect:

```json
{
  "email": "user@test.com",
  "role": "Admin"
}
```

Correct:

```
Register user

      |

      v

Assign Customer role
```

Admin users are created separately.

---

# Ownership Security

## Decision

Customers can only access their own reservations.

## Reason

Authorization must protect user data.

Implementation concept:

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

Rules:

Customer:

- Can access own reservations

Admin:

- Can access all reservations

---

# Current User Service

## Decision

The current authenticated user is accessed through an abstraction.

Example:

```csharp
ICurrentUserService
```

## Reason

Application logic should not directly depend on:

- HttpContext
- ASP.NET Core API details

This keeps application logic testable.

---

# Swagger JWT Support

## Decision

Swagger is configured to support JWT authentication.

## Reason

Allows testing protected endpoints directly from Swagger UI.

Features:

- Bearer token input
- Authorization header handling
- Testing authenticated requests

Flow:

```
Login

↓

Copy JWT

↓

Swagger Authorize

↓

Call protected endpoints
```

---

# Authentication and Authorization Middleware

## Decision

Authentication and authorization middleware are explicitly configured.

```csharp
app.UseAuthentication();

app.UseAuthorization();
```

## Reason

Authentication must happen before authorization.

Flow:

```
Request

    |

    v

Authentication

"Who is this user?"

    |

    v

Authorization

"Is this user allowed?"

    |

    v

Endpoint
```

---

# Testing Strategy

## Decision

Tests are separated according to architectural boundaries.

Technologies:

- xUnit
- FluentAssertions
- Moq
- WebApplicationFactory
- SQLite In-Memory

---

# Domain Tests

## Decision

Domain logic is tested with unit tests.

Purpose:

- Test business rules
- Test invariants
- Test entity behavior

No:

- Database
- API
- Framework dependencies

Reason:

The domain should be independently testable.

---

# Application Tests

## Decision

Application use cases are tested with unit tests.

External dependencies are mocked.

Example:

```
CreateReservation

        |

        v

Mock Repository
```

Reason:

Application logic can be tested without infrastructure.

---

# Integration Tests

## Decision

Integration tests verify complete application behavior.

Tests include:

- API endpoints
- JWT authentication
- Authorization
- Middleware
- EF Core integration

---

# WebApplicationFactory

## Decision

ASP.NET Core integration tests use WebApplicationFactory.

## Reason

It creates a realistic test version of the application.

It uses:

```
TestServer
```

which acts as an in-memory HTTP server.

---

# SQLite In-Memory Database for Tests

## Decision

Integration tests use SQLite In-Memory instead of the development database.

## Reason

Benefits:

- Tests are isolated
- No developer database changes
- Faster execution
- Repeatable tests

The database exists only during the test lifetime.

---

# AI-Assisted Development

## Decision

AI tools are used as development assistance.

AI is used for:

- Generating boilerplate code
- Suggesting implementations
- Reviewing code
- Improving documentation

## Human Responsibilities

The developer remains responsible for:

- Architecture decisions
- Reviewing generated code
- Testing
- Final implementation decisions

Generated code is reviewed before committing.

---

# Summary of Main Principles

The project follows these principles:

- Separation of concerns
- Dependency inversion
- Domain-driven design concepts
- Thin controllers
- Secure authentication and authorization
- Testable business logic
- Infrastructure isolation
- Clear responsibility boundaries