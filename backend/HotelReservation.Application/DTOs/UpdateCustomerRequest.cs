using System;
using System.Collections.Generic;
using System.Text;

namespace HotelReservation.Application.DTOs;

// Currently the same shape as CustomerDto minus Id — left as its own type rather than
// reusing CustomerDto, since request (input) and DTO (output) serve different purposes
// even when they happen to coincide today; collapsing them risks over-posting-style
// coupling later if CustomerDto ever gains a response-only field.
public class UpdateCustomerRequest
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }
}
