namespace api.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using api.Models;
using api.Data;
using Azure.Identity;

public class ImageUploadService
{
    private readonly string _storagePath;
    private readonly ImageDbContext _context;

    public ImageUploadService(ImageDbContext context)
    {
        _context = context;
        //TODO: investigate why it doesn't save to DB, but uploading to the uploads folder in wwwroot works
        _storagePath = Path.Combine("wwwroot", "uploads");

        if(!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    //TODO: check the IFormFile Config to make sure that it cooperates with everything else --  I was having issues with other IFormFile. Seems to work tho
    public async Task<string> SaveImageAsync(FileUploadItem item)
    {
        try
        {
            var file = item.File;

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if(!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Invalid file type.");
            }

            item.UnixTime = new DateTimeOffset(item.DateTime).ToUnixTimeSeconds();

            var filePath = Path.Combine(_storagePath, item.UnixTime.ToString());

            await using(var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
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

            return filePath;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[ERROR] [ImageUploadService.cs] [SaveImageAsync]: Exception message: {ex.Message}");
            throw;
        }
    }
}