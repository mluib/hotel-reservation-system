using HotelReservation.Application.DTOs;
using HotelReservation.Application.Hotels;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HotelController : ControllerBase
{
    private readonly GetHotel _getHotel;
    private readonly UpdateHotel _updateHotel;

    public HotelController(GetHotel getHotel, UpdateHotel updateHotel)
    {
        _getHotel = getHotel;
        _updateHotel = updateHotel;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dto = await _getHotel.ExecuteAsync();
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPut]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(UpdateHotelRequest request)
    {
        await _updateHotel.ExecuteAsync(request);
        return Ok();
    }
}
