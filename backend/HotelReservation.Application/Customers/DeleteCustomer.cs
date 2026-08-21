using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Customers;

public class DeleteCustomer
{
    private readonly ICustomerRepository _repository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IAuthService _authService;

    public DeleteCustomer(ICustomerRepository repository, IReservationRepository reservationRepository, IAuthService authService)
    {
        _repository = repository;
        _reservationRepository = reservationRepository;
        _authService = authService;
    }

    public async Task ExecuteAsync(System.Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new NotFoundException("Customer not found.");

        var hasReservations = await _reservationRepository.ExistsForCustomerAsync(id);
        if (hasReservations)
            throw new ConflictException("Cannot delete a customer that has reservations.");

        await _repository.DeleteAsync(existing);

        // Also revoke the linked Identity login, not just the domain profile -- otherwise
        // the account can still authenticate with no Customer record behind it (blocked as
        // of AuthService.LoginAsync's own check, but better not to leave the orphaned
        // login sitting there at all). Not wrapped in one transaction with the delete
        // above (Identity's UserManager manages its own persistence) -- an unlikely
        // failure here leaves an orphaned-but-now-rejected login, not a data-loss risk.
        if (!string.IsNullOrWhiteSpace(existing.IdentityUserId))
            await _authService.DeleteUserAsync(existing.IdentityUserId!);
    }
}
