## Example: Reservation Validation

### Prompt

Implement reservation validation according to the existing Clean Architecture.

Requirements:

- Prevent overlapping reservations for the same room.
- Validate that check-out date is after check-in date.
- Check that customer and room exist before creating a reservation.
- Keep dependency direction unchanged.

### Result

Implemented:

- Reservation validation in the reservation use case.
- Repository methods required for existence and availability checks.
- Infrastructure implementations using EF Core.

### Review and adjustments

The generated implementation was reviewed.

Decisions made:

- Domain rules remain inside the Reservation entity.
- External state checks (customer/room existence and availability) are handled through repositories.
- Application layer coordinates the workflow without depending directly on EF Core.


## Example: Customer CRUD Implementation

### Prompt

Implement Customer CRUD following the existing Clean Architecture pattern in this project.

Requirements:
- Follow the existing Reservation implementation style.
- Keep dependency direction unchanged:
  API → Application → Domain
  Infrastructure implements Application interfaces.
- Create the required application services, repository interface, repository implementation, DTOs and API endpoints.

### Result

Implemented:

- CreateCustomer use case
- GetCustomerById use case
- GetCustomers use case
- UpdateCustomer use case
- DeleteCustomer use case
- Customer repository interface and implementation
- Customer API controller
- DTOs for API requests and responses

### Review and adjustments

The generated implementation was reviewed and adjusted.

Changes made:
- Removed reflection-based ID assignment.
- Added a domain method for updating customer data while preserving domain rules.
- Verified dependency direction and Clean Architecture boundaries.