using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

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

builder.Services.AddSqlServer<ImageDbContext>(builder.Configuration.GetConnectionString("ImageDb"));

builder.Services.AddScoped<ArchiveManager>();

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

        Console.WriteLine($"DEBUG [Program.cs] [/api/archive/start]: endpoint hit: request: {request.StartDate} {request.EndDate}");

        if(request == null)
        {
            return Results.BadRequest();
        }

        Guid jobId = manager.StartArchive(request);

        if(jobId == Guid.Empty)
        {
            return Results.Problem("Error creating new archive job.");
        }

        ArchiveRequest job = manager.GetJob(jobId);

        return Results.Accepted($"/api/archive/request/{jobId}", job); //TODO: 'Created' response?
    }
    catch(Exception exception)
    {
        Console.WriteLine($"ERROR [Program.cs] [/api/archive/start]: Exception message: {exception.Message}");
        return Results.Problem();
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
        return Results.Problem(exception.Message);
    }
})
.WithSummary("Retrieves an archive job status.")
.Produces<ArchiveRequest>(200, "application/json");

app.MapGet("/api/archive/download/{jobId}", (ArchiveManager manager, Guid jobId) =>
{
    try
    {
        string filePath = manager.GetFilePath(jobId);

        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        return Results.File(fileStream, "application/zip", $"{jobId}.zip");
    }
    catch(Exception exception)
    {
        Console.WriteLine($"ERROR [Program.cs] [/api/archive/download]: Exception message: {exception.Message}");
        return Results.Problem(exception.Message);
    }
})
.WithSummary("Requests an archive download.")
.Produces<FileResult>(200, "application/zip");

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
        return Results.Problem(exception.Message);
    }
})
.WithSummary("Retrieves a list of all images stored in the database.")
.Produces<List<Image>>(200, "application/json");

app.MapGet("/api/images/paginated", async (ImageDbContext dbContext, string filter) =>
{
    try
    {
        var parameters = filter.Split(',');

        if( !DateTime.TryParse(parameters[0], out DateTime startDate) ||
            !DateTime.TryParse(parameters[1], out DateTime endDate) ||
            !int.TryParse(parameters[2], out int pageIndex) ||
            !int.TryParse(parameters[3], out int pageSize)
        )
        {
            return Results.BadRequest();
        }

        string site = parameters[4];
        var query = dbContext.Images
            .Where(i => i.DateTime >= startDate && i.DateTime <= endDate && i.Site == site)
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
        return Results.Problem(exception.Message);
    }
})
.WithSummary("Retrieves a single image based on a given id value.")
.Produces<FileResult>(200, "image/jpeg");

app.Run();