namespace HotelReservation.Application.Interfaces;

public interface IImageStorageService
{
    /// <summary>
    /// Saves the image content under the given subfolder using the given file name,
    /// overwriting any existing file with the same name. Returns the relative URL
    /// the file can be served from (e.g. "/uploads/rooms/{fileName}").
    /// </summary>
    Task<string> SaveAsync(Stream content, string fileName, string subfolder);

    /// <summary>
    /// Deletes a previously saved image, given the relative URL returned by SaveAsync.
    /// No-op if the file does not exist.
    /// </summary>
    Task DeleteAsync(string relativeUrl);
}
