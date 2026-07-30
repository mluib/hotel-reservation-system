using HotelReservation.Application.DTOs;
using HotelReservation.Application.Reservations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly CreateReservation _createReservation;
    private readonly GetReservations _getReservations;
    private readonly GetReservationById _getReservationById;
    private readonly DeleteReservation _deleteReservation;
    private readonly GetMyReservations _getMyReservations;

    public ReservationsController(
        CreateReservation createReservation,
        GetReservations getReservations,
        GetReservationById getReservationById,
        DeleteReservation deleteReservation,
        GetMyReservations getMyReservations)
    {
        _createReservation = createReservation;
        _getReservations = getReservations;
        _getReservationById = getReservationById;
        _deleteReservation = deleteReservation;
        _getMyReservations = getMyReservations;
    }


    [HttpPost]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(
        CreateReservationRequest request)
    {
        var id = await _createReservation.ExecuteAsync(request);
        var dto = await _getReservationById.ExecuteAsync(id);

        return CreatedAtAction(nameof(GetById), new { id = id }, dto);
    }

    [HttpGet]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _getReservations.ExecuteAsync();
        return Ok(list);
    }

    [HttpGet("mine")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMine()
    {
        var list = await _getMyReservations.ExecuteAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getReservationById.ExecuteAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        await _deleteReservation.ExecuteAsync(id);
        return Ok();
    }
}