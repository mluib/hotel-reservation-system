using HotelReservation.Application.DTOs;
using HotelReservation.Application.Rooms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly CreateRoom _createRoom;
    private readonly GetRooms _getRooms;
    private readonly GetRoomById _getRoomById;
    private readonly UpdateRoom _updateRoom;
    private readonly DeleteRoom _deleteRoom;

    public RoomsController(
        CreateRoom createRoom,
        GetRooms getRooms,
        GetRoomById getRoomById,
        UpdateRoom updateRoom,
        DeleteRoom deleteRoom)
    {
        _createRoom = createRoom;
        _getRooms = getRooms;
        _getRoomById = getRoomById;
        _updateRoom = updateRoom;
        _deleteRoom = deleteRoom;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateRoomRequest request)
    {
        var id = await _createRoom.ExecuteAsync(request);
        var dto = await _getRoomById.ExecuteAsync(id);
        return CreatedAtAction(nameof(GetById), new { id = id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _getRooms.ExecuteAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getRoomById.ExecuteAsync(id);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(System.Guid id, UpdateRoomRequest request)
    {
        await _updateRoom.ExecuteAsync(id, request);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        await _deleteRoom.ExecuteAsync(id);
        return Ok();
    }
}
