using HotelReservation.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.Enums;

namespace HotelReservation.Domain.Entities;

public class Room
{
    public Guid Id { get; private set; }

    public string Number { get; private set; }

    public RoomType Type { get; private set; }

    public decimal PricePerNight { get; private set; }

    public Guid HotelId { get; private set; }

    public string? ImageUrl { get; private set; }

    public List<Reservation> Reservations { get; private set; }


    public Room(
        string number,
        RoomType type,
        decimal pricePerNight,
        Guid hotelId)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Room number is required.");

        if (pricePerNight <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        Id = Guid.NewGuid();
        Number = number;
        Type = type;
        PricePerNight = pricePerNight;
        HotelId = hotelId;
        Reservations = new List<Reservation>();
    }

    public void Update(string number, RoomType type, decimal pricePerNight, Guid hotelId)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Room number is required.");

        if (pricePerNight <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        Number = number;
        Type = type;
        PricePerNight = pricePerNight;
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