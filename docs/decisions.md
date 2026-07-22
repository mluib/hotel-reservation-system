# Architecture Decisions

This document records important design decisions made during the development of the Hotel Reservation System.

## Clean Architecture

### Decision

The application is structured using Clean Architecture with separate projects:

```
HotelReservation.Api
HotelReservation.Application
HotelReservation.Domain
HotelReservation.Infrastructure
```

### Reason

The goal is to separate business rules from technical details.

Benefits:

- Clear separation of responsibilities
- Easier testing
- Maintainable code structure
- Independent business logic

### Dependency Direction

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

The Domain layer has no dependency on other layers.

---

## Domain Entities Own Business Rules

### Decision

Business rules are implemented inside domain entities.

Examples:

- Reservation dates must be valid.
- Customer data must satisfy business rules.
- Room updates must preserve valid state.

### Reason

Business rules should not depend on controllers, databases, or frameworks.

---

## Domain Update Methods

### Decision

Entities provide update methods instead of replacing objects.

Example:

```csharp
customer.Update(firstName, lastName, email);
```

### Reason

This keeps validation and business rules inside the domain model.

Reflection-based updates were avoided because they bypass encapsulation.

---

## Use Cases in Application Layer

### Decision

Controllers call application services/use cases.

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

### Reason

Controllers should only handle HTTP concerns.

Business workflows belong in the Application layer.

---

## DTOs Between API and Application

### Decision

The API does not expose domain entities directly.

DTOs are used for communication:

```
CreateCustomerRequest
UpdateCustomerRequest
CustomerDto
```

### Reason

Benefits:

- API contracts are independent from domain models
- Prevents accidental data exposure
- Allows future changes without affecting domain entities

---

## Repository Pattern

### Decision

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

### Reason

The Application layer should not depend directly on Entity Framework Core.

Benefits:

- Better testability
- Separation of concerns
- Flexible data access implementation

---

## Entity Framework Core and SQL Server

### Decision

Entity Framework Core is used for database access.

### Reason

Benefits:

- Native .NET integration
- Database migrations
- Strong typing
- Reduced database boilerplate

---

## REST API Design

### Decision

API endpoints follow REST conventions.

Example:

```
GET    /api/customers
POST   /api/customers

GET    /api/customers/{id}
PUT    /api/customers/{id}
DELETE /api/customers/{id}
```

### Reason

HTTP methods represent operations:

| Operation | HTTP Method |
|-----------|-------------|
| Create | POST |
| Read | GET |
| Update | PUT |
| Delete | DELETE |

---

## Validation Separation

### Decision

Validation responsibilities are separated by layer.

Domain layer:

- Entity rules
- Valid object state

Application layer:

- Checking external state
- Customer existence
- Room availability

Infrastructure layer:

- Database queries

Example:

```
Domain:
"Is this reservation valid?"

Application:
"Can this reservation be created?"

Infrastructure:
"How do I access the database?"
```

---

## AI-Assisted Development

### Decision

AI tools are used as development assistance.

AI is used for:

- Generating boilerplate code
- Suggesting implementations
- Reviewing code
- Improving documentation

### Human Responsibilities

The developer remains responsible for:

- Architecture decisions
- Reviewing generated code
- Testing
- Final implementation decisions

Generated code is reviewed before committing.