using HotelReservation.Application.DTOs;
using HotelReservation.Application.Rooms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

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
    private readonly UploadRoomImage _uploadRoomImage;

    public RoomsController(
        CreateRoom createRoom,
        GetRooms getRooms,
        GetRoomById getRoomById,
        UpdateRoom updateRoom,
        DeleteRoom deleteRoom,
        UploadRoomImage uploadRoomImage)
    {
        _createRoom = createRoom;
        _getRooms = getRooms;
        _getRoomById = getRoomById;
        _updateRoom = updateRoom;
        _deleteRoom = deleteRoom;
        _uploadRoomImage = uploadRoomImage;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(RoomRequest request)
    {
        var id = await _createRoom.ExecuteAsync(request);
        var dto = await _getRoomById.ExecuteAsync(id);
        return CreatedAtAction(nameof(GetById), new { id = id }, dto);
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<RoomDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] RoomFilterRequest filter)
    {
        var list = await _getRooms.ExecuteAsync(filter);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(System.Guid id)
    {
        var dto = await _getRoomById.ExecuteAsync(id);
        return Ok(dto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(System.Guid id, RoomRequest request)
    {
        await _updateRoom.ExecuteAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(System.Guid id)
    {
        await _deleteRoom.ExecuteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<RoomDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(System.Guid id, IFormFile file)
    {
        var request = new ImageUploadRequest
        {
            Content = file.OpenReadStream(),
            ContentType = file.ContentType,
            Length = file.Length
        };

        var dto = await _uploadRoomImage.ExecuteAsync(id, request);
        return Ok(dto);
    }
}
