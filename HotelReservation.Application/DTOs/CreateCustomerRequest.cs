using System;
using System.Collections.Generic;
using System.Text;

namespace HotelReservation.Application.DTOs;

public class CreateCustomerRequest
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }
}
