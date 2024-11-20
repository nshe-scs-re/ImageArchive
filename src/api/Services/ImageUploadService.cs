namespace api.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using api.Models;
using api.Data;


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
    public async Task<string> SaveImageAsync(IFormFile file)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_storagePath, uniqueFileName);

            await using(var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            //Save image metadata to the database
            var image = new Image
            {
                Name = fileName,
                FilePath = filePath,
                DateTime = DateTime.UtcNow,
                UnixTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync(); 
            return uniqueFileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR [ImageUploadService.cs] [SaveImageAsync]: Exception message: {ex.Message}");
            throw;
        }
    }
}
