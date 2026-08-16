using System.Collections.Generic;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;

namespace HotelReservation.Application.Reservations;

public class GetMyReservations
{
    private readonly IReservationRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly HotelReservation.Application.Interfaces.ICustomerRepository _customerRepository;

    public GetMyReservations(IReservationRepository repository, ICurrentUserService currentUser, HotelReservation.Application.Interfaces.ICustomerRepository customerRepository)
    {
        _repository = repository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
    }

    public async Task<IEnumerable<ReservationDto>> ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthenticatedException("Unauthenticated");

        var customer = await _customerRepository.GetByIdentityUserIdAsync(_currentUser.UserId!);
        if (customer == null) throw new NotFoundException("Customer does not exist.");

        var reservations = await _repository.GetByCustomerIdAsync(customer.Id);

        var list = new List<ReservationDto>();
        foreach (var r in reservations)
        {
            list.Add(new ReservationDto
            {
                Id = r.Id,
                RoomId = r.RoomId,
                CustomerId = r.CustomerId,
                CheckIn = r.CheckIn,
                CheckOut = r.CheckOut,
                Status = r.Status,
                PricePerNight = r.PricePerNight
            });
        }

        return list;
    }
}
