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
        while(!Jobs.TryAdd((Guid)request.Id, request));
    }

    public ArchiveRequest ProcessArchiveRequest(ArchiveRequest request)
    {
        RegisterJob(request);

        #pragma warning disable CS4014 // Allow the async method to run without awaiting it. This is intentional.
        CreateArchiveAsync(request);
        #pragma warning restore CS4014

        return request;
    }

    public ArchiveRequest CancelArchiveRequest(ArchiveRequest request)
    {
        if(request.Id is not null)
        {
            request = GetJob((Guid)request.Id);
            request.Status = ArchiveStatus.Canceled;
        }

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

            List<Image> images = await dbContext.Images
                .Where
                (
                    i => i.DateTime >= request.StartDateTime
                    && i.DateTime <= request.EndDateTime
                    && i.SiteName == request.SiteName
                    && i.SiteNumber == request.SiteNumber
                    && i.CameraPositionNumber == request.CameraPositionNumber
                )
                .ToListAsync();

            request.TotalImages = images.Count;

            string archiveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "archives");

            Directory.CreateDirectory(archiveDirectory);

            request.FilePath = Path.Combine(Directory.GetCurrentDirectory(), "archives", $"{request.Id}.zip");

            ConcurrentBag<Exception> exceptions = new ConcurrentBag<Exception>();

            using(FileStream zipToOpen = new FileStream(request.FilePath, FileMode.Create))
            {
                using(ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    object archiveLock = new object();

                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 2
                    };

                    Parallel.ForEach(images, parallelOptions, (image, state) =>
                    {
                        if(request.Status == ArchiveStatus.Canceled)
                        {
                            Console.WriteLine($"[INFO] [ArchiveManager] [CreateArchiveAsync]: Archive status of {request.Id} is {request.Status}. Breaking loop state.");
                            state.Break();
                            return;
                        }

                        request.IncrementProcessedImages();

                        if(request.ProcessedImages % 10 == 0 && request.TotalImages > 0)
                        {
                            double processingProgress = (double)request.ProcessedImages / request.TotalImages;
                            if(processingProgress > 0.01)
                            {
                                TimeSpan elapsedTime = stopwatch.Elapsed;
                                TimeSpan estimatedTotalTime = TimeSpan.FromTicks((long)(elapsedTime.Ticks / processingProgress));
                                TimeSpan estimatedTimeRemaining = estimatedTotalTime - elapsedTime;

                                request.ElapsedTime = elapsedTime;
                                request.EstimatedTimeRemaining = estimatedTimeRemaining;
                            }
                        }

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

            if(request.Status == ArchiveStatus.Processing)
            {
                request.Status = ArchiveStatus.Completed;
                Console.WriteLine($"[INFO] [ArchiveManager] [CreateArchiveAsync]: Archiving process completed for id {request.Id}. Elapsed Time: {stopwatch.Elapsed}");
            }
            else
            {
                Console.WriteLine($"[INFO] [ArchiveManager] [CreateArchiveAsync]: Archiving status for id {request.Id}: {request.Status}. Elapsed Time: {stopwatch.Elapsed}");
            }
        }
    }
}
