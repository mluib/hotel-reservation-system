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

    [HttpPost("image")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImage(Microsoft.AspNetCore.Http.IFormFile file)
    {
        try
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
