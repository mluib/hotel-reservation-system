using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Customers;

public class DeleteCustomer
{
    private readonly ICustomerRepository _repository;
    private readonly IReservationRepository _reservationRepository;

    public DeleteCustomer(ICustomerRepository repository, IReservationRepository reservationRepository)
    {
        _repository = repository;
        _reservationRepository = reservationRepository;
    }

    public async Task ExecuteAsync(System.Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new InvalidOperationException("Customer not found.");

        var hasReservations = await _reservationRepository.ExistsForCustomerAsync(id);
        if (hasReservations)
            throw new InvalidOperationException("Cannot delete a customer that has reservations.");

        await _repository.DeleteAsync(existing);
    }
}
