using System;
using System.Collections.Generic;
using System.Text;

namespace HotelReservation.Domain.Entities;

public class Hotel
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public List<Room> Rooms { get; private set; }


    public Hotel(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Hotel name is required.");

        Id = Guid.NewGuid();
        Name = name;
        Address = address;
        Rooms = new List<Room>();
    }


    public void AddRoom(Room room)
    {
        Rooms.Add(room);
    }
}