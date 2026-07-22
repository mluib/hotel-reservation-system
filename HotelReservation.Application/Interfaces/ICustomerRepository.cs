using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Interfaces;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer);

    Task<Customer?> GetByIdAsync(Guid id);

    Task<IEnumerable<Customer>> GetAllAsync();

    Task UpdateAsync(Customer customer);

    Task DeleteAsync(Customer customer);
}
