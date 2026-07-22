using HotelReservation.Application.DTOs;
using HotelReservation.Application.Reservations;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly CreateReservation _createReservation;
    private readonly GetReservations _getReservations;
    private readonly GetReservationById _getReservationById;
    private readonly DeleteReservation _deleteReservation;

    public ReservationsController(
        CreateReservation createReservation,
        GetReservations getReservations,
        GetReservationById getReservationById,
        DeleteReservation deleteReservation)
    {
        _createReservation = createReservation;
        _getReservations = getReservations;
        _getReservationById = getReservationById;
        _deleteReservation = deleteReservation;
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        CreateReservationRequest request)
    {
        var id = await _createReservation.ExecuteAsync(request);
        var dto = await _getReservationById.ExecuteAsync(id);

        return CreatedAtAction(nameof(GetById), new { id = id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _getReservations.ExecuteAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getReservationById.ExecuteAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        await _deleteReservation.ExecuteAsync(id);
        return Ok();
    }
}