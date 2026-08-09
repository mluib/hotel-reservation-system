using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Customers;

// Lets a logged-in Customer fetch their own profile, mirroring how GetMyReservations
// scopes reservations to the current user instead of requiring an id.
public class GetCurrentCustomer
{
    private readonly ICustomerRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentCustomer(ICustomerRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<CustomerDto?> ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new InvalidOperationException("Unauthenticated user has no profile.");

        var customer = await _repository.GetByIdentityUserIdAsync(_currentUser.UserId!);
        if (customer == null)
            return null;

        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email
        };
    }
}
