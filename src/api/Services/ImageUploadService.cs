namespace api.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using api.Models;
using api.Data;

public class ImageUploadService
{
    private readonly string _basePath;
    private readonly ImageDbContext _context;

    public ImageUploadService(ImageDbContext context)
    {
        _context = context;
        _basePath = "images";
    }

    public async Task<Image> SaveImageAsync(FileUploadItem item)
    {
        try
        {
            var allowedFileExtensions = new[] { ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(item.File.FileName).ToLowerInvariant();
            if(!allowedFileExtensions.Contains(fileExtension))
            {
                Console.WriteLine($"[ERROR] [ImageUploadService.cs] [SaveImageAsync]: Invalid file type.");
                throw new InvalidOperationException("[ERROR] [ImageUploadService.cs] [SaveImageAsync]: Invalid file type.");
            }

            item.UnixTime = new DateTimeOffset(item.DateTime).ToUnixTimeSeconds();

            var unixEpoch = new DateTime(1970, 1, 1);

            var daysSinceEpoch = (item.DateTime - unixEpoch).Days;

            var siteNumberString = $"site_{item.SiteNumber}";

            var directoryPath = Path.Combine(_basePath, item.SiteName.ToLower(), siteNumberString, daysSinceEpoch.ToString());

            if(!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, $"{item.UnixTime}{fileExtension}");

            await using(var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await item.File.CopyToAsync(fileStream);
            }

            var image = new Image
            {
                FilePath = filePath,
                UnixTime = item.UnixTime,
                DateTime = item.DateTime,
                SiteName = item.SiteName,
                SiteNumber = item.SiteNumber,
                CameraPositionNumber = item.CameraPositionNumber,
                CameraPositionName = item.CameraPositionName
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            return image;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[ERROR] [ImageUploadService.cs] [SaveImageAsync]: Exception message: {ex.Message}");
            throw;
        }
    }
}