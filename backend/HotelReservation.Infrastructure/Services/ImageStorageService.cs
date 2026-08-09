using HotelReservation.Application.Interfaces;

namespace HotelReservation.Infrastructure.Services;

// Stores images on local disk under <webRootPath>/uploads/<subfolder>/<fileName>.
// webRootPath is supplied by the Api layer (wwwroot), keeping this project free
// of a direct dependency on ASP.NET Core hosting abstractions.
public class ImageStorageService : IImageStorageService
{
    private readonly string _webRootPath;

    public ImageStorageService(string webRootPath)
    {
        _webRootPath = webRootPath;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string subfolder)
    {
        var folder = Path.Combine(_webRootPath, "uploads", subfolder);
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, fileName);
        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream);
        }

        return $"/uploads/{subfolder}/{fileName}";
    }

    public Task DeleteAsync(string relativeUrl)
    {
        var relativePath = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_webRootPath, relativePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
