using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Customers;

public class CreateCustomer
{
    private readonly ICustomerRepository _repository;

    public CreateCustomer(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> ExecuteAsync(CreateCustomerRequest request)
    {
        var customer = new Customer(request.FirstName, request.LastName, request.Email);

        await _repository.AddAsync(customer);
        return customer.Id;
    }
}
