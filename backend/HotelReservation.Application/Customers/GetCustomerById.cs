using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Customers;

public class GetCustomerById
{
    private readonly ICustomerRepository _repository;

    public GetCustomerById(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CustomerDto> ExecuteAsync(System.Guid id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            throw new NotFoundException("Customer not found.");

        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email.Value
        };
    }
}
