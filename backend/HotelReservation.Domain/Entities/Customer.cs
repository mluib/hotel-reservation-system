using System;
using System.Collections.Generic;
using System.Text;

namespace HotelReservation.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }

    // IdentityUser.Id from ASP.NET Identity (string). Separate from the domain GUID Id.
    public string? IdentityUserId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public List<Reservation> Reservations { get; private set; }


    public Customer(
        string firstName,
        string lastName,
        string email,
        string? identityUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        IdentityUserId = identityUserId;
        Reservations = new List<Reservation>();
    }

    public void Update(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.");

        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }
}