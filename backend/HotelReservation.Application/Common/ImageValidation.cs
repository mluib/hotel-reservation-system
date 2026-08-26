using HotelReservation.Application.Common.Exceptions;
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

    // Magic-byte signatures for the three allowed types. The Content-Type header alone
    // (checked in Validate() below) is client-supplied and trivially spoofable -- these
    // are what actually confirm the bytes are what they claim to be.
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] RiffTag = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF", WebP's outer container
    private static readonly byte[] WebpTag = { 0x57, 0x45, 0x42, 0x50 }; // "WEBP", at offset 8 within RIFF

    /// <summary>
    /// Validates an image upload: required, an allowed content type, under the size
    /// cap, and its actual bytes matching the signature for its declared content type
    /// (the Content-Type header alone is client-supplied and trivially spoofable).
    /// </summary>
    /// <returns>
    /// A seekable, rewound copy of the upload's full content -- the original stream is
    /// forward-only and gets consumed checking the signature, so callers must save this
    /// one instead of <see cref="ImageUploadRequest.Content"/>.
    /// </returns>
    public static async Task<Stream> ValidateAsync(ImageUploadRequest request)
    {
        if (request.Length <= 0)
            throw new ValidationException("Image file is required.");

        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new ValidationException("Unsupported image type. Allowed types: image/jpeg, image/png, image/webp.");

        if (request.Length > MaxSizeBytes)
            throw new ValidationException("Image exceeds the maximum allowed size of 5MB.");

        // The size cap above already bounds this, so buffering the whole upload into
        // memory here is cheap.
        var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer);
        buffer.Position = 0;

        var header = new byte[12];
        var read = await buffer.ReadAsync(header.AsMemory(0, header.Length));
        buffer.Position = 0;

        var matchesDeclaredType = request.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(PngSignature),
            "image/webp" => read >= 12
                && header.AsSpan(0, 4).SequenceEqual(RiffTag)
                && header.AsSpan(8, 4).SequenceEqual(WebpTag),
            _ => false // unreachable -- Validate() above already rejected any other content type
        };

        if (!matchesDeclaredType)
            throw new ValidationException("The uploaded file's content does not match its declared image type.");

        return buffer;
    }

    public static string GetExtension(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => throw new ValidationException("Unsupported image type.")
    };
}
