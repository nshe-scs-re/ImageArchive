using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

//Console.WriteLine($"DEBUG [Program.cs]: Connection string is: {builder.Configuration.GetConnectionString("ImageDb")}");

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Image Archive API",
        Version = "v1",
        Description = "An ASP.NET Core 8 minimal Web API for managing an image archive."
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSqlServer<ImageDbContext>(builder.Configuration.GetConnectionString("ImageDb"));

builder.Services.AddScoped<ArchiveManager>();

builder.Services.AddScoped<ImageUploadService>();

builder.Services.AddAntiforgery(); //TODO: Configure antiforgery options

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        //TODO: Investigate further configuration options for added security
        builder.AllowAnyOrigin()
        //builder.WithOrigins("10.176.244.111")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

});

var app = builder.Build();

app.UseCors();

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery(); // Investigate ways to disable for development environment

app.MapGet("/api/db-verify", async(ImageDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if(canConnect)
        {
            Console.WriteLine($"DEBUG [Program.cs] [/db-verify]: Database connection succeded!");
            return Results.Ok("Database connection succeeded.");
        }
        else
        {
            Console.WriteLine($"DEBUG [Program.cs] [/db-verify]: Database connection failed.");
            return Results.Problem("Database connection failed.");
        }
    }
    catch(Exception exception)
    {
        Console.WriteLine($"DEBUG [Program.cs] [/api/db-verify]: Database connection failed - {exception.Message}");
        return Results.Problem(exception.Message);
    }
})
.WithSummary("Tests the ability for the API host to connect to the image database.")
.Produces<IResult>(200, "application/json");

app.MapPost("/api/archive/request", async (ArchiveManager manager, HttpContext context) =>
{
    try
    {
        ArchiveRequest? request = await context.Request.ReadFromJsonAsync<ArchiveRequest>();

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

app.MapGet("/api/archive/status/{jobId}", (ArchiveManager manager, Guid jobId) =>
{
    try
    {
        ArchiveRequest job = manager.GetJob(jobId);

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

app.MapGet("/api/archive/download/{jobId}", (ArchiveManager manager, Guid jobId) =>
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

        FileStream fileStream = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read);

        return Results.File(fileStream, "application/zip", $"{jobId}.zip");
    }
    catch(Exception exception)
    {
        Console.WriteLine($"ERROR [Program.cs] [/api/archive/download]: Exception message: {exception.Message}");
        return Results.Problem(detail: exception.Message, statusCode: 500);
    }
})
.WithSummary("Requests an archive download.")
.Produces<FileResult>(200, "application/zip")
.Produces<ArchiveRequest>(404, "application/json")
.Produces<ArchiveRequest>(409, "application/json")
.Produces(500);

app.MapGet("/api/images/all", async (ImageDbContext dbContext) =>
{
    try
    {
        List<Image> images = await dbContext.Images.ToListAsync();

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

app.MapGet("/api/images/paginated", async (ImageDbContext dbContext, string filter) =>
{
    try
    {
        var parameters = filter.Split(',');

        if
        ( 
            !DateTime.TryParse(parameters[0], out DateTime startDate) ||
            !DateTime.TryParse(parameters[1], out DateTime endDate) ||
            !int.TryParse(parameters[2], out int pageIndex) ||
            !int.TryParse(parameters[3], out int pageSize)
        )
        {
            return Results.BadRequest();
        }

        string siteName = parameters[4];
        int.TryParse(parameters[5], out int siteNumber);
        int.TryParse(parameters[6], out int cameraPosition);

        var query = dbContext.Images
            .Where(i => 
                i.DateTime >= startDate && 
                i.DateTime <= endDate && 
                i.SiteName == siteName && 
                i.SiteNumber == siteNumber && 
                i.CameraPositionNumber == cameraPosition)
            .OrderBy(i => i.Id);

        int totalCount = await query.CountAsync();

        var images = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Results.Ok(new {TotalCount = totalCount, Images = images });
    }
    catch(Exception exception)
    {
        Console.WriteLine($"ERROR [Program.cs] [/api/images/paginated]: Exception message: {exception.Message}");
        return Results.Problem(exception.Message);
    }
})
.WithSummary("Retrieves a small list of images to be later retrieved in paginated results.")
.Produces<List<Image>>(200, "application/json");

app.MapGet("/api/images/{id}", async (ImageDbContext dbContext, long id) =>
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
        string extension = Path.GetExtension(image.FilePath)!.ToLowerInvariant();
        string mimeType = extension switch
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
        //return Results.Problem(exception.Message);
        return Results.Problem("not found");
    }
})
.WithSummary("Retrieves a single image based on a given id value.")
.Produces<FileResult>(200, "image/jpeg");


app.MapPost("/api/upload/multiple", async (HttpRequest request, ImageUploadService imageService) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var files = form.Files;
        if(files == null || files.Count == 0)
        {
            return Results.BadRequest("No files uploaded or files are empty.");
        }

        //int? cameraPosition = null;
        if(!int.TryParse(form["cameraPosition"], out int cameraPosition))
        {
            return Results.BadRequest("Invalid image camera position.");
        }

        string? siteName = form["site"];

        if(siteName == null)
        {
            return Results.BadRequest("Invalid image site.");
        }

        //int? siteNumber = null;
        if (!int.TryParse(form["siteNumber"], out int siteNumber))
        {
            return Results.BadRequest("Invalid image site number.");
        }

        string? cameraPositionName = form["cameraPositionName"];

        if(cameraPositionName == null)
        {
            return Results.BadRequest("Invalid image camera position name.");
        }

        var imageJson = form["image"];
        if(string.IsNullOrEmpty(imageJson))
        {
            return Results.BadRequest("Image metadata is missing.");
        }
        //where the code starts breaking
        var image = JsonSerializer.Deserialize<Image>(imageJson);
        if(image == null)
        {
            return Results.BadRequest("Invalid image metadata.");
        }
        List<string> savedFileNames = new List<string>();
        foreach(var file in files)
        {
            var savedFileName = await imageService.SaveImageAsync(file, image);
            savedFileNames.Add(savedFileName);
        }

        return Results.Ok(new { Message = "Upload successful!", FileNames = savedFileNames });
    }
    catch(Exception ex)
    {
        return Results.Problem("An error occurred while processing the file upload: " + ex.Message);
    }
});
app.Run();
public partial class Program { }
