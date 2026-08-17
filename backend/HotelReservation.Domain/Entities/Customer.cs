using System;
using System.Collections.Generic;
using System.Text;

using HotelReservation.Domain.ValueObjects;

namespace HotelReservation.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }

    // IdentityUser.Id from ASP.NET Identity (string). Separate from the domain GUID Id.
    public string? IdentityUserId { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public EmailAddress Email { get; private set; }

    public List<Reservation> Reservations { get; private set; }

    /// <summary>
    /// EF Core materialization constructor.
    /// </summary>
    /// <remarks>
    /// The business constructor below no longer binds 1:1 to the mapped columns now that
    /// Email is <see cref="EmailAddress"/> rather than string, so EF falls back to this
    /// parameterless constructor plus setting the private-setter properties directly via
    /// reflection instead of constructor injection.
    /// </remarks>
    private Customer() { }

    public Customer(
        string firstName,
        string lastName,
        string email,
        string? identityUserId = null)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = new EmailAddress(email);
        IdentityUserId = identityUserId;
        Reservations = new List<Reservation>();
    }

    public void Update(string firstName, string lastName, string email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = new EmailAddress(email);
    }
}