using HotelReservation.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Enums;
using HotelReservation.Domain.ValueObjects;

namespace HotelReservation.Domain.Entities;

public class Room
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Number { get; private set; }

    public RoomType Type { get; private set; }

    public Money PricePerNight { get; private set; }

    public Guid HotelId { get; private set; }

    public string? ImageUrl { get; private set; }

    /// <summary>
    /// EF Core materialization constructor -- see <see cref="Customer"/>'s for why this is
    /// needed now that PricePerNight is <see cref="Money"/> rather than decimal.
    /// </summary>
    private Room() { }

    public Room(
        string name,
        string number,
        RoomType type,
        decimal pricePerNight,
        Guid hotelId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name is required.");

        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Room number is required.");

        Id = Guid.NewGuid();
        Name = name;
        Number = number;
        Type = type;
        PricePerNight = new Money(pricePerNight);
        HotelId = hotelId;
    }

    public void Update(string name, string number, RoomType type, decimal pricePerNight, Guid hotelId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name is required.");

        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Room number is required.");

        Name = name;
        Number = number;
        Type = type;
        PricePerNight = new Money(pricePerNight);
        HotelId = hotelId;
    }

    public void SetImage(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Image URL is required.");

        ImageUrl = url;
    }

    public void ClearImage()
    {
        ImageUrl = null;
    }
}