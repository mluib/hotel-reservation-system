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

    public CreateReservation(
        IReservationRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(
        CreateReservationRequest request)
    {
        // Validate dates
        if (request.CheckOut <= request.CheckIn)
            throw new InvalidOperationException("Check-out must be after check-in.");

        // Ensure room exists
        var roomExists = await _repository.RoomExistsAsync(request.RoomId);
        if (!roomExists)
            throw new InvalidOperationException("Room does not exist.");

        // Ensure customer exists
        var customerExists = await _repository.CustomerExistsAsync(request.CustomerId);
        if (!customerExists)
            throw new InvalidOperationException("Customer does not exist.");

        // Prevent overlapping reservations for the same room
        var overlapping = await _repository.HasOverlappingReservationAsync(
            request.RoomId,
            request.CheckIn,
            request.CheckOut);

        if (overlapping)
            throw new InvalidOperationException("Room is already reserved for this period.");

        var reservation = new Reservation(
            request.RoomId,
            request.CustomerId,
            request.CheckIn,
            request.CheckOut);

        await _repository.AddAsync(reservation);
    }
}