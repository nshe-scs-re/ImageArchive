﻿using ExtractImagePaths.Data;
using ExtractImagePaths.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

string? environment = Environment.GetEnvironmentVariable("RUNTIME_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>(optional: true, reloadOnChange: true)
            .Build();

var serviceProvider = new ServiceCollection()
    .AddSqlServer<ImageDbContext>(configuration.GetConnectionString("ImageDb"))
    .AddLogging(configure => configure.AddConsole())
    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information)
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<Program>>();

logger.LogInformation("Environment is set to: {environment}.", environment);

string rootPath = configuration["Paths:RootPath"]!;

try
{
    logger.LogInformation("Root directory is set to: {rootPath}", rootPath);
    if(Directory.Exists(rootPath))
    {
        logger.LogInformation("Root directory exists. Finding subdirectories...");

        string[] directories = Directory.GetDirectories(rootPath);

        logger.LogInformation("Found {directories.Length} subdirectories:\n\t{listOfDirectories}", directories.Length, string.Join("\n\t", directories));
    }
    else
    {
        logger.LogInformation("{rootPath} does not exist. Exiting.", rootPath);
        return;
    }
}
catch(Exception ex)
{
    logger.LogError("An error occurred: {ex.Message}", ex.Message);
    return;
}

logger.LogInformation("Finding image paths...");

var imagePaths = GetImagePaths(rootPath);

logger.LogInformation("Found {imagePaths.Count} image paths.", imagePaths.Count);

InsertImagePathsIntoDatabase(imagePaths, serviceProvider);

List<string> GetImagePaths(string rootPath)
{
    List<string> imagePaths = [];

    List<string> imageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"];

    foreach(var directory in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
    {
        foreach(var extension in imageExtensions)
        {
            foreach(var file in Directory.GetFiles(directory, $"*{extension}"))
            {
                imagePaths.Add(file);
            }
        }
    }

    return imagePaths;
}

void InsertImagePathsIntoDatabase(List<string> imagePaths, ServiceProvider serviceProvider)
{
    List<string> siteNames = new List<string>
    {
        "Sheep",
        "Snake",
        "Sagehen",
        "Spring",
        "Conness",
        "Rockland",
        "Lassen"
    };

    using(var scope = serviceProvider.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ImageDbContext>();

        var images = imagePaths.Select(imagePath =>
        {
            string name = Path.GetFileName(imagePath);

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);

            if(nameWithoutExtension.Length <= 2)
            {
                logger.LogWarning("Skipping file with short name: {imagePath}", imagePath);
                return (IsValid: false, Image: null as Image);
            }

            string unixTimeString = nameWithoutExtension[..^2];

            if(!long.TryParse(unixTimeString, out long unixTime))
            {
                logger.LogWarning("Skipping file with invalid Unix time: {imagePath}", imagePath);
                return (IsValid: false, Image: null as Image);
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

            string siteName = siteNames.FirstOrDefault(s => imagePath.Contains(s)) ?? string.Empty;

            int camera = imagePath.Contains("Camera2") ? 2 : 1;

            var image = new Image
            {
                Name = name,
                FilePath = imagePath,
                DateTime = dateTime,
                UnixTime = unixTime,
                Site = siteName,
                Camera = camera
            };

            return (IsValid: true, Image: image);

        })
        .Where(result => result.IsValid)
        .Select(result => result.Image)
        .ToList();

        if(images[0] is not null)
        {
            logger.LogInformation(
                "Example insertion:\n\t" +
                "Name: {Name}\n\t" +
                "File Path: {FilePath}\n\t" +
                "Unix Time: {UnixTime}\n\t" +
                "DateTime: {DateTime}\n\t" +
                "Site: {Site}\n\t" +
                "Camera: {Camera}\n\t" +
                "Camera Position: {CameraPosition}",
                images[0]!.Name,
                images[0]!.FilePath,
                images[0]!.UnixTime,
                images[0]!.DateTime,
                images[0]!.Site,
                images[0]!.Camera,
                images[0]!.CameraPosition
            );
        }
        else
        {
            logger.LogError("No images available. Exiting...");
            return;
        }

        Thread.Sleep(1000);

        Console.WriteLine("\nContinue with insertion? (Y/N):");

        string choice = Console.ReadLine()!.Trim().ToUpper();

        while(choice is not "Y" and not "N")
        {
            logger.LogError("Invalid input. Please enter Y or N:");
            choice = Console.ReadLine()!.Trim().ToUpper();
        }

        if(choice == "N")
        {
            logger.LogInformation("Exiting...");
            return;
        }

        context.Images.AddRange(images!);

        int addedCount = context.SaveChanges();

        logger.LogInformation("Insertion complete. {addedCount} insertions.", addedCount);
    }
}
