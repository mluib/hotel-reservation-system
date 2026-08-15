using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Application.DTOs;
using HotelReservation.Application.Interfaces;
using HotelReservation.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HotelReservation.Application.Reservations;

public class CreateReservation
{
    private readonly IReservationRepository _repository;
    private readonly IRoomRepository _roomRepository;
    private readonly HotelReservation.Application.Interfaces.ICurrentUserService _currentUser;
    private readonly HotelReservation.Application.Interfaces.ICustomerRepository _customerRepository;
    private readonly ILogger<CreateReservation> _logger;

    public CreateReservation(
        IReservationRepository repository,
        IRoomRepository roomRepository,
        HotelReservation.Application.Interfaces.ICurrentUserService currentUser,
        HotelReservation.Application.Interfaces.ICustomerRepository customerRepository,
        ILogger<CreateReservation> logger)
    {
        _repository = repository;
        _roomRepository = roomRepository;
        _currentUser = currentUser;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<Guid> ExecuteAsync(
        CreateReservationRequest request)
    {
        // Validate dates
        if (request.CheckOut <= request.CheckIn)
            throw new InvalidOperationException("Check-out must be after check-in.");

        // Ensure room exists, and read its current price to snapshot onto the reservation
        var room = await _roomRepository.GetByIdAsync(request.RoomId);
        if (room == null)
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
        {
            _logger.LogWarning(
                "Reservation rejected: room {RoomId} already booked for {CheckIn:d} - {CheckOut:d}",
                request.RoomId, request.CheckIn, request.CheckOut);
            throw new InvalidOperationException("Room is already reserved for this period.");
        }

        var reservation = new Reservation(
            request.RoomId,
            customerId,
            request.CheckIn,
            request.CheckOut,
            room.PricePerNight);

        await _repository.AddAsync(reservation);

        _logger.LogInformation(
            "Reservation {ReservationId} created for room {RoomId}, customer {CustomerId}, {CheckIn:d} - {CheckOut:d}",
            reservation.Id, request.RoomId, customerId, request.CheckIn, request.CheckOut);

        return reservation.Id;
    }
}