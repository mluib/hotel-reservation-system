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
    private readonly UploadHotelImage _uploadHotelImage;

    public HotelController(GetHotel getHotel, UpdateHotel updateHotel, UploadHotelImage uploadHotelImage)
    {
        _getHotel = getHotel;
        _updateHotel = updateHotel;
        _uploadHotelImage = uploadHotelImage;
    }

    [HttpGet]
    [ProducesResponseType<HotelDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get()
    {
        var dto = await _getHotel.ExecuteAsync();
        return Ok(dto);
    }

    [HttpPut]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(UpdateHotelRequest request)
    {
        await _updateHotel.ExecuteAsync(request);
        return NoContent();
    }

    [HttpPost("image")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    [ProducesResponseType<HotelDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(Microsoft.AspNetCore.Http.IFormFile file)
    {
        var request = new ImageUploadRequest
        {
            Content = file.OpenReadStream(),
            ContentType = file.ContentType,
            Length = file.Length
        };

        var dto = await _uploadHotelImage.ExecuteAsync(request);
        return Ok(dto);
    }
}
