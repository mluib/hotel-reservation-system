using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Customers;

public class DeleteCustomer
{
    private readonly ICustomerRepository _repository;

    public DeleteCustomer(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(System.Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new InvalidOperationException("Customer not found.");

        await _repository.DeleteAsync(existing);
    }
}
