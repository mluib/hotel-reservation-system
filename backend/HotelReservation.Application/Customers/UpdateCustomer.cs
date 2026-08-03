using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Customers;

public class UpdateCustomer
{
    private readonly ICustomerRepository _repository;

    public UpdateCustomer(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(System.Guid id, UpdateCustomerRequest request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            throw new InvalidOperationException("Customer not found.");

        // Use domain method to update allowed fields and preserve invariants
        existing.Update(request.FirstName, request.LastName, request.Email);

        await _repository.UpdateAsync(existing);
    }
}
