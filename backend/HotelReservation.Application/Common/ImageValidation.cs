using HotelReservation.Application.DTOs;

namespace HotelReservation.Application.Common;

// Shared validation for image uploads, used by both UploadRoomImage and UploadHotelImage.
internal static class ImageValidation
{
    public const long MaxSizeBytes = 5 * 1024 * 1024; // 5MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static void Validate(ImageUploadRequest request)
    {
        if (request.Length <= 0)
            throw new InvalidOperationException("Image file is required.");

        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new InvalidOperationException("Unsupported image type. Allowed types: image/jpeg, image/png, image/webp.");

        if (request.Length > MaxSizeBytes)
            throw new InvalidOperationException("Image exceeds the maximum allowed size of 5MB.");
    }

    public static string GetExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => throw new InvalidOperationException("Unsupported image type.")
    };
}
