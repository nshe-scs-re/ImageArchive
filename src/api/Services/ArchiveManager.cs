using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;

namespace api.Services;

public class ArchiveManager(IServiceScopeFactory DbScopeFactory)
{
    private static readonly ConcurrentDictionary<Guid, ArchiveRequest> Jobs = new();

    private void RegisterJob(ArchiveRequest request)
    {
        do
        {
            request.Id = Guid.NewGuid();
        }
        while(!Jobs.TryAdd(request.Id, request));
    }

    public ArchiveRequest ProcessArchiveRequest(ArchiveRequest request)
    {
        RegisterJob(request);

        #pragma warning disable CS4014 // Allow the async method to run without awaiting it. This is intentional.
        CreateArchiveAsync(request);
        #pragma warning restore CS4014

        return request;
    }

    public ArchiveRequest GetJob(Guid jobId)
    {
        return Jobs.TryGetValue(jobId, out ArchiveRequest? request)
            ? request
            : throw new KeyNotFoundException($"No archive process found with ID: {jobId}");
    }

    private async Task CreateArchiveAsync(ArchiveRequest request)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        Console.WriteLine($"[INFO] [ArchiveManager] [CreateArchiveAsync]: Archive process started.");

        request.Status = ArchiveStatus.Processing;

        using(IServiceScope DbScope = DbScopeFactory.CreateScope())
        {
            ImageDbContext dbContext = DbScope.ServiceProvider.GetRequiredService<ImageDbContext>();

            //TODO: Extend LINQ query to include other search parameters
            List<Image> images = await dbContext.Images
                .Where(i => i.DateTime >= request.StartDate && i.DateTime <= request.EndDate)
                .ToListAsync();

            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Archives");

            string zipFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Archives", $"{request.Id}.zip");

            if(!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            request.FilePath = zipFilePath;

            ConcurrentBag<Exception> exceptions = new ConcurrentBag<Exception>();

            using(FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create))
            {
                using(ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    object archiveLock = new object();

                    Parallel.ForEach(images, (image) =>
                    {
                        int year = image.DateTime.Value.Year;
                        string month = $"{image.DateTime:MMM}";
                        string day = $"{image.DateTime:dd}";

                        try
                        {
                            if(File.Exists(image.FilePath))
                            {
                                lock(archiveLock)
                                {
                                    ZipArchiveEntry entry = archive.CreateEntry($"{year}/{month}/{day}/{day} {month} {year} {image.DateTime:hh.mmtt}.{Path.GetExtension(image.FilePath)}");

                                    using(FileStream fileStream = new FileStream(image.FilePath, FileMode.Open, FileAccess.Read))
                                    {
                                        using(Stream entryStream = entry.Open())
                                        {
                                            fileStream.CopyTo(entryStream);
                                        }
                                    }
                                }

                            }
                            else
                            {
                                Console.WriteLine($"DEBUG [ArchiveManager.cs]: {image.FilePath} does not exist."); //TODO: Implement logging
                            }
                        }
                        catch(Exception exception)
                        {
                            exceptions.Add(exception);
                        }
                    });

                    if(!exceptions.IsEmpty)
                    {
                        foreach(Exception exception in exceptions)
                        {
                            Console.WriteLine($"DEBUG [ArchiveManager.cs]: Exception: {exception.Message}"); //TODO: Implement logging

                            request.AddError(exception.Message);
                        }
                    }
                }
            }

            stopwatch.Stop();

            Console.WriteLine($"[INFO] [ArchiveManager] [CreateArchiveAsync]: Archiving process complete. Elapsed Time: {stopwatch.Elapsed}");

            request.Status = ArchiveStatus.Completed;
        }
    }
}
