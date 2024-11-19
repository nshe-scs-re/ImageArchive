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
        _storagePath = Path.Combine("wwwroot", "uploads");

        if(!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> SaveImageAsync(IFormFile file)
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
}
