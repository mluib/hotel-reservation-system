using System;
using System.Collections.Generic;
using System.Text;

using System.ComponentModel.DataAnnotations;

namespace HotelReservation.Application.DTOs;

/// <summary>
/// Currently the same shape as <see cref="CustomerDto"/> minus Id -- left as its own type
/// rather than reusing <see cref="CustomerDto"/>, since request (input) and DTO (output)
/// serve different purposes even when they happen to coincide today; collapsing them risks
/// over-posting-style coupling later if <see cref="CustomerDto"/> ever gains a response-only
/// field.
/// </summary>
public class UpdateCustomerRequest
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
