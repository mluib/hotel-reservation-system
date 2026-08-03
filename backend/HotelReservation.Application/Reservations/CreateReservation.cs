using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;

namespace HotelReservation.Application.Reservations;

public class CreateReservation
{
    private readonly IReservationRepository _repository;
    private readonly HotelReservation.Application.Interfaces.ICurrentUserService _currentUser;
    private readonly HotelReservation.Application.Interfaces.ICustomerRepository _customerRepository;

    public CreateReservation(
        IReservationRepository repository,
        HotelReservation.Application.Interfaces.ICurrentUserService currentUser,
        HotelReservation.Application.Interfaces.ICustomerRepository customerRepository)
    {
        _repository = repository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
    }

    public async Task<Guid> ExecuteAsync(
        CreateReservationRequest request)
    {
        // Validate dates
        if (request.CheckOut <= request.CheckIn)
            throw new InvalidOperationException("Check-out must be after check-in.");

        // Ensure room exists
        var roomExists = await _repository.RoomExistsAsync(request.RoomId);
        if (!roomExists)
            throw new InvalidOperationException("Room does not exist.");

        // Determine customer id from the authenticated user. Clients must not provide CustomerId.
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new InvalidOperationException("Unauthenticated user cannot create a reservation.");

        // Look up domain customer by the IdentityUserId (string) rather than parsing the identity id as a GUID.
        var customer = await _customerRepository.GetByIdentityUserIdAsync(_currentUser.UserId!);
        if (customer == null)
            throw new InvalidOperationException("Customer does not exist.");

        var customerId = customer.Id;

        // Prevent overlapping reservations for the same room
        var overlapping = await _repository.HasOverlappingReservationAsync(
            request.RoomId,
            request.CheckIn,
            request.CheckOut);

        if (overlapping)
            throw new InvalidOperationException("Room is already reserved for this period.");

        var reservation = new Reservation(
            request.RoomId,
            customerId,
            request.CheckIn,
            request.CheckOut);

        await _repository.AddAsync(reservation);
        return reservation.Id;
    }
}