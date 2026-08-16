using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HotelReservation.Application.Reservations;

public class CancelReservation
{
    private readonly IReservationRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CancelReservation> _logger;

    public CancelReservation(
        IReservationRepository repository,
        ICurrentUserService currentUser,
        ICustomerRepository customerRepository,
        ILogger<CancelReservation> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthenticatedException("Unauthenticated user cannot cancel a reservation.");

        var reservation = await _repository.GetByIdAsync(id);
        if (reservation == null)
        {
            _logger.LogWarning("Cancel rejected: reservation {ReservationId} not found", id);
            throw new NotFoundException("Reservation not found.");
        }

        // Admins can cancel any reservation; customers only their own.
        if (!_currentUser.IsInRole("Admin"))
        {
            var customer = await _customerRepository.GetByIdentityUserIdAsync(_currentUser.UserId!);
            if (customer == null)
                throw new NotFoundException("Customer does not exist.");

            if (reservation.CustomerId != customer.Id)
            {
                _logger.LogWarning(
                    "Cancel rejected: reservation {ReservationId} does not belong to customer {CustomerId}",
                    id, customer.Id);
                throw new ForbiddenException("Reservation does not belong to the current customer.");
            }
        }

        reservation.Cancel();
        await _repository.UpdateAsync(reservation);

        _logger.LogInformation("Reservation {ReservationId} cancelled", id);
    }
}
