using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Enums;
using HotelReservation.Domain.ValueObjects;

namespace HotelReservation.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }

    public Guid RoomId { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateRange Stay { get; private set; }

    public ReservationStatus Status { get; private set; }

    public Money PricePerNight { get; private set; }

    /// <summary>
    /// EF Core materialization constructor -- see <see cref="Customer"/>'s for why this is
    /// needed now that Stay/PricePerNight are <see cref="DateRange"/>/<see cref="Money"/>
    /// rather than raw DateTime/decimal.
    /// </summary>
    private Reservation() { }

    public Reservation(
        Guid roomId,
        Guid customerId,
        DateTime checkIn,
        DateTime checkOut,
        decimal pricePerNight)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        CustomerId = customerId;
        Stay = new DateRange(checkIn, checkOut);
        Status = ReservationStatus.Confirmed;
        PricePerNight = new Money(pricePerNight);
    }


    public void Cancel()
    {
        Status = ReservationStatus.Cancelled;
    }
}