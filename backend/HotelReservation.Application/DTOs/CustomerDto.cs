using System;
using System.Collections.Generic;
using System.Text;

namespace HotelReservation.Application.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }
}
