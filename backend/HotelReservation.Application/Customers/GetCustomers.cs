using System.Collections.Generic;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Customers;

public class GetCustomers
{
    private readonly ICustomerRepository _repository;

    public GetCustomers(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CustomerDto>> ExecuteAsync()
    {
        var customers = await _repository.GetAllAsync();

        var list = new List<CustomerDto>();

        foreach (var c in customers)
        {
            list.Add(new CustomerDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email.Value
            });
        }

        return list;
    }
}
