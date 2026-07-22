using HotelReservation.Application.DTOs;
using HotelReservation.Application.Reservations;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly CreateReservation _createReservation;

    public ReservationsController(
        CreateReservation createReservation)
    {
        _createReservation = createReservation;
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        CreateReservationRequest request)
    {
        await _createReservation.ExecuteAsync(request);

        return Ok();
    }
}