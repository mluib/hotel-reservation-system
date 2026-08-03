using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Enums;

namespace HotelReservation.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }

    public Guid RoomId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTime CheckIn { get; private set; }

    public DateTime CheckOut { get; private set; }

    public ReservationStatus Status { get; private set; }

    public decimal PricePerNight { get; private set; }


    public Reservation(
        Guid roomId,
        Guid customerId,
        DateTime checkIn,
        DateTime checkOut,
        decimal pricePerNight)
    {
        if (checkOut <= checkIn)
            throw new ArgumentException(
                "Check-out must be after check-in.");

        Id = Guid.NewGuid();
        RoomId = roomId;
        CustomerId = customerId;
        CheckIn = checkIn;
        CheckOut = checkOut;
        Status = ReservationStatus.Confirmed;
        PricePerNight = pricePerNight;
    }


    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
    }
}