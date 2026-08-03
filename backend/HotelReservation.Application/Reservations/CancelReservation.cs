using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Reservations;

public class CancelReservation
{
    private readonly IReservationRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICustomerRepository _customerRepository;

    public CancelReservation(
        IReservationRepository repository,
        ICurrentUserService currentUser,
        ICustomerRepository customerRepository)
    {
        _repository = repository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
    }

    public async Task ExecuteAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new InvalidOperationException("Unauthenticated user cannot cancel a reservation.");

        var reservation = await _repository.GetByIdAsync(id);
        if (reservation == null)
            throw new InvalidOperationException("Reservation not found.");

        // Admins can cancel any reservation; customers only their own.
        if (!_currentUser.IsInRole("Admin"))
        {
            var customer = await _customerRepository.GetByIdentityUserIdAsync(_currentUser.UserId!);
            if (customer == null)
                throw new InvalidOperationException("Customer does not exist.");

            if (reservation.CustomerId != customer.Id)
                throw new InvalidOperationException("Reservation does not belong to the current customer.");
        }

        reservation.Cancel();
        await _repository.UpdateAsync(reservation);
    }
}
