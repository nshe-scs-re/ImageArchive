﻿using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api;

public static class EndpointsMap
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/upload", async (HttpRequest request, ImageUploadService imageService) =>
        {
            if(!request.HasFormContentType)
            {
                return Results.BadRequest();
            }

            var requestForm = request.Form;

            var requestFile = request.Form.Files[0];

            if(requestFile == null)
            {
                return Results.BadRequest();
            }

            var fileUploadItem = new FileUploadItem
            {
                File = requestFile
            };

            if(requestForm.TryGetValue("DateTime", out var dateTimeValues) && DateTime.TryParse(dateTimeValues.FirstOrDefault(), out var dateTime))
            {
                fileUploadItem.DateTime = dateTime;
            }

            if(requestForm.TryGetValue("SiteName", out var siteName))
            {
                fileUploadItem.SiteName = siteName!;
            }

            if(requestForm.TryGetValue("SiteNumber", out var siteNumberValue) && int.TryParse(siteNumberValue.FirstOrDefault(), out var siteNumber))
            {
                fileUploadItem.SiteNumber = siteNumber;
            }

            if(requestForm.TryGetValue("CameraPositionNumber", out var cameraPositionNumberValues) && int.TryParse(cameraPositionNumberValues.FirstOrDefault(), out var cameraPositionNumber))
            {
                fileUploadItem.CameraPositionNumber = cameraPositionNumber;
            }

            if(requestForm.TryGetValue("CameraPositionName", out var cameraPositionName))
            {
                fileUploadItem.CameraPositionName = cameraPositionName!;
            }

            var image = await imageService.SaveImageAsync(fileUploadItem);

            return Results.Ok(image);
        });

        builder.MapPost("/api/log-query", async (HttpContext context, ImageDbContext dbContext) =>
        {
            var userQuery = await context.Request.ReadFromJsonAsync<UserQuery>();

            if(userQuery is null)
            {
                return Results.Problem();
            }

            try
            {
                dbContext.UserQueries.Add(userQuery);
                await dbContext.SaveChangesAsync();
                Console.WriteLine("UserQuery successfully saved to database.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error saving UserQuery: {ex.Message}");
                return Results.Problem();
            }

            return Results.Accepted();
        })
        .WithSummary("Logs user search queries")
        .Produces<UserQuery>(200)
        .Produces(500);

        builder.MapGet("/api/query-history", async (HttpContext context, ImageDbContext dbContext) =>
        {
            try
            {
                var history = await dbContext.UserQueries
                    .OrderByDescending(q => q.Timestamp)
                    .ToListAsync();

                if(history.Count == 0)
                {
                    return Results.NotFound();
                }

                return Results.Ok(history);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error fetching query history: {ex.Message}");
                return Results.Problem();
            }
        })
        .WithSummary("Retrieves the authenticated user's query history")
        .Produces<List<UserQuery>>(200)
        .Produces(404)
        .Produces(500);

        builder.MapGet("/api/db-verify", async (ImageDbContext dbContext) =>
        {
            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync();

                if(canConnect)
                {
                    //Console.WriteLine($"[INFO] [Program.cs] [/db-verify]: Database connection succeded!");
                    return Results.Ok();
                }
                else
                {
                    //Console.WriteLine($"[info] [Program.cs] [/db-verify]: Database connection failed.");
                    return Results.Problem();
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine($"[ERROR] [Program.cs] [/api/db-verify]: Exception: {exception.Message}");
                return Results.Problem();
            }
        })
        .WithSummary("Tests the ability for the API host to connect to the image database.")
        .Produces<IResult>(200, "application/json");

        builder.MapPost("/api/archive/request", async (ArchiveManager manager, HttpContext context) =>
        {
            try
            {
                var request = await context.Request.ReadFromJsonAsync<ArchiveRequest>();

                if(request is null)
                {
                    return Results.BadRequest();
                }

                request = manager.ProcessArchiveRequest(request);

                return Results.Accepted($"/api/archive/request/{request.Id}", request);
            }
            catch(Exception exception)
            {
                Console.WriteLine($"ERROR [Program.cs] [/api/archive/start]: Exception message: {exception.Message}");
                return Results.Problem(detail: exception.Message);
            }
        })
        .WithSummary("Starts an archive process.")
        .Produces<ArchiveRequest>(200, "application/json")
        .Accepts<ArchiveRequest>("application/json");

        builder.MapGet("/api/archive/status/{jobId}", (ArchiveManager manager, Guid jobId) =>
        {
            try
            {
                var job = manager.GetJob(jobId);

                return Results.Ok(job);
            }
            catch(Exception exception)
            {
                Console.WriteLine($"ERROR [Program.cs] [/api/archive/status/{jobId}]: Exception message: {exception.Message}");
                return Results.Problem(detail: exception.Message);
            }
        })
        .WithSummary("Retrieves an archive job status.")
        .Produces<ArchiveRequest>(200, "application/json");

        builder.MapGet("/api/archive/download/{jobId}", (ArchiveManager manager, Guid jobId) =>
        {
            try
            {
                var request = manager.GetJob(jobId);

                if(request is null || string.IsNullOrEmpty(request.FilePath) || !File.Exists(request.FilePath))
                {
                    return Results.NotFound(request);
                }

                if(request.Status is not ArchiveStatus.Completed)
                {
                    return Results.Conflict(request);
                }

                var fileStream = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read);

                return Results.Stream(fileStream, "application/zip", $"{request.SiteName}_{request.SiteNumber}_archive_{DateTime.Now}.zip");
            }
            catch(Exception exception)
            {
                Console.WriteLine($"ERROR [Program.cs] [/api/archive/download]: Exception message: {exception.Message}");
                return Results.Problem();
            }
        })
        .WithSummary("Requests an archive download.")
        .Produces<FileResult>(200, "application/zip")
        .Produces<ArchiveRequest>(404, "application/json")
        .Produces<ArchiveRequest>(409, "application/json")
        .Produces(500);

        builder.MapGet("/api/images/all", async (ImageDbContext dbContext) =>
        {
            try
            {
                var images = await dbContext.Images.ToListAsync();

                return Results.Ok(images);
            }
            catch(Exception exception)
            {
                Console.WriteLine($"ERROR [Program.cs] [/api/images/all]: Exception message: {exception.Message}");
                return Results.Problem(detail: exception.Message, statusCode: 500);
            }
        })
        .WithSummary("Retrieves a list of all images stored in the database.")
        .Produces<List<Image>>(200, "application/json");

        builder.MapGet("/api/images/paginated", async (ImageDbContext dbContext, string filter) =>
        {
            try
            {
                var parameters = filter.Split(',');

                if
                (
                    !DateTime.TryParse(parameters[0], out var startDate) ||
                    !DateTime.TryParse(parameters[1], out var endDate) ||
                    !int.TryParse(parameters[2], out var pageIndex) ||
                    !int.TryParse(parameters[3], out var pageSize)
                )
                {
                    return Results.BadRequest();
                }

                var siteName = parameters[4];
                int.TryParse(parameters[5], out var siteNumber);
                int.TryParse(parameters[6], out var cameraPosition);

                var query = dbContext.Images
                    .Where(i =>
                        i.DateTime >= startDate &&
                        i.DateTime <= endDate &&
                        i.SiteName == siteName &&
                        i.SiteNumber == siteNumber &&
                        i.CameraPositionNumber == cameraPosition)
                    .OrderBy(i => i.DateTime);

                var totalCount = await query.CountAsync();

                var images = await query
                    .Skip(pageIndex * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Results.Ok(new { TotalCount = totalCount, Images = images });
            }
            catch(Exception exception)
            {
                Console.WriteLine($"ERROR [Program.cs] [/api/images/paginated]: Exception message: {exception.Message}");
                return Results.Problem(exception.Message);
            }
        })
        .WithSummary("Retrieves a small list of images to be later retrieved in paginated results.")
        .Produces<List<Image>>(200, "application/json");

        builder.MapGet("/api/images/{id}", async (ImageDbContext dbContext, long id) =>
        {
            try
            {
                var image = await dbContext.Images.FindAsync(id);
                if(image is null)
                {
                    Console.WriteLine($"DEBUG [Program.cs] [/api/images/id]: image with Id {id} is null.");
                    return Results.NotFound();
                }

                var fileStream = new FileStream(image.FilePath!, FileMode.Open, FileAccess.Read);
                var extension = Path.GetExtension(image.FilePath)!.ToLowerInvariant();
                var mimeType = extension switch
                {
                    ".jpeg" or ".jpg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                return Results.File(fileStream, mimeType);
            }
            catch(Exception exception)
            {
                Console.WriteLine($"ERROR [Program.cs] [/api/images/id]: Exception message: {exception.Message}");
                return Results.Problem();
            }
        })
        .WithSummary("Retrieves a single image based on a given id value.")
        .Produces<FileResult>(200, "image/jpeg");

        builder.MapPost("/api/archive/cancel/{jobId}", async (ArchiveManager manager, HttpContext context) =>
        {
            var request = await context.Request.ReadFromJsonAsync<ArchiveRequest>();

            if(request is null)
            {
                return Results.NotFound();
            }

            // TODO: More case coverage
            request = manager.CancelArchiveRequest(request);

            return Results.Ok(request);
        });

        return builder;
    }
}
