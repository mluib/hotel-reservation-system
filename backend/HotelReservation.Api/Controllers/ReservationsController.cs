using HotelReservation.Application.DTOs;
using HotelReservation.Application.Reservations;
using Microsoft.AspNetCore.Authorization;
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
    private readonly GetMyReservations _getMyReservations;
    private readonly CancelReservation _cancelReservation;

    public ReservationsController(
        CreateReservation createReservation,
        GetReservations getReservations,
        GetReservationById getReservationById,
        DeleteReservation deleteReservation,
        GetMyReservations getMyReservations,
        CancelReservation cancelReservation)
    {
        _createReservation = createReservation;
        _getReservations = getReservations;
        _getReservationById = getReservationById;
        _deleteReservation = deleteReservation;
        _getMyReservations = getMyReservations;
        _cancelReservation = cancelReservation;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateReservationRequest request)
    {
        var id = await _createReservation.ExecuteAsync(request);
        var dto = await _getReservationById.ExecuteAsync(id);

        return CreatedAtAction(nameof(GetById), new { id = id }, dto);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<IEnumerable<ReservationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        var list = await _getReservations.ExecuteAsync();
        return Ok(list);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType<IEnumerable<ReservationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine()
    {
        var list = await _getMyReservations.ExecuteAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getReservationById.ExecuteAsync(id);
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        await _deleteReservation.ExecuteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Customer,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(System.Guid id)
    {
        await _cancelReservation.ExecuteAsync(id);
        return NoContent();
    }
}
