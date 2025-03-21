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

        Console.WriteLine($"[INFO] [ArchiveManager] [CreateArchiveAsync]: Archive process started for id {request.Id}");

        request.Status = ArchiveStatus.Processing;

        using(IServiceScope DbScope = DbScopeFactory.CreateScope())
        {
            ImageDbContext dbContext = DbScope.ServiceProvider.GetRequiredService<ImageDbContext>();

            //TODO: Extend LINQ query to include other search parameters
            List<Image> images = await dbContext.Images
                .Where(i => i.DateTime >= request.StartDate && i.DateTime <= request.EndDate)
                .ToListAsync();

            request.FilePath = Path.Combine(Directory.GetCurrentDirectory(), "archives", $"{request.Id}.zip");

            ConcurrentBag<Exception> exceptions = new ConcurrentBag<Exception>();

            using(FileStream zipToOpen = new FileStream(request.FilePath, FileMode.Create))
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
                                    ZipArchiveEntry entry = archive.CreateEntry($"{year}/{month}/{day}/{day}_{month}_{year}_{image.DateTime:hh.mmtt}{Path.GetExtension(image.FilePath)}");

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
                                Console.WriteLine($"[WARNING] [ArchiveManager.cs] [CreateArchiveAsync]: {image.FilePath} does not exist.");
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
                            Console.WriteLine($"[ERROR] [ArchiveManager.cs] [CreateArchiveAsync]: Exception: {exception.Message}");

                            request.AddError(exception.Message);
                        }
                    }
                }
            }

            stopwatch.Stop();

            request.Status = ArchiveStatus.Completed;

            Console.WriteLine($"[INFO] [ArchiveManager] [CreateArchiveAsync]: Archiving process completed for id {request.Id}. Elapsed Time: {stopwatch.Elapsed}");
        }
    }
}
