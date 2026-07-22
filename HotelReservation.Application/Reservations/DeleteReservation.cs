using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Reservations;

public class DeleteReservation
{
    private readonly IReservationRepository _repository;

    public DeleteReservation(IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(System.Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) throw new InvalidOperationException("Reservation not found.");

        await _repository.DeleteAsync(existing);
    }
}
