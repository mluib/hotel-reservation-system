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
        var overlapping =
            await _repository.HasOverlappingReservationAsync(
                request.RoomId,
                request.CheckIn,
                request.CheckOut);


        if (overlapping)
        {
            throw new InvalidOperationException(
                "Room is already reserved for this period.");
        }


        var reservation = new Reservation(
            request.RoomId,
            request.CustomerId,
            request.CheckIn,
            request.CheckOut);


        await _repository.AddAsync(reservation);
    }
}