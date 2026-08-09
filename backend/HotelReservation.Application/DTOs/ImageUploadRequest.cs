namespace HotelReservation.Application.DTOs;

// Transport-agnostic representation of an uploaded image, so the Application layer
// does not depend on ASP.NET Core's IFormFile. The Api layer maps IFormFile to this.
public class ImageUploadRequest
{
    public required Stream Content { get; set; }

    public required string ContentType { get; set; }

    public long Length { get; set; }
}
