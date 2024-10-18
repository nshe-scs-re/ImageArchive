using ExtractImagePaths.Data;
using ExtractImagePaths.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var directoryRootPath = Environment.GetEnvironmentVariable("NEVCAN_DIRECTORY_ROOT_PATH");

if(string.IsNullOrEmpty(directoryRootPath))
{
    return;
}

directoryRootPath = Environment.ExpandEnvironmentVariables(directoryRootPath);

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

try
{

    var serviceProvider = new ServiceCollection()
        .AddSqlServer<ImageDbContext>(configuration.GetConnectionString("ImageDb"))
        .AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        })
        .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information)
        .BuildServiceProvider();

    var logger = serviceProvider.GetService<ILogger<Program>>();

    using(var scope = serviceProvider.CreateScope())
    {
        var context = scope.ServiceProvider.GetService<ImageDbContext>();

        if(context.Database.CanConnect())
        {
            logger.LogInformation("Database connection success!");
        }
        else
        {
            logger.LogError("Could not connect to database. Exiting application.");
            return;
        }
    }

    logger.LogInformation("Environment is set to: {environment}.", environment);

    try
    {
        logger.LogInformation("Root directory is set to: {directoryRootPath}", directoryRootPath);
        if(Directory.Exists(directoryRootPath))
        {
            logger.LogInformation("Root directory exists. Finding subdirectories...");

            string[] directories = Directory.GetDirectories(directoryRootPath);

            logger.LogInformation("Found {directories.Length} subdirectories:\n\t{listOfDirectories}", directories.Length, string.Join("\n\t", directories));
        }
        else
        {
            logger.LogInformation("{directoryRootPath} does not exist. Exiting.", directoryRootPath);
            return;
        }
    }
    catch(Exception ex)
    {
        logger.LogError("{ex.Message}", ex.Message);
        return;
    }

    logger.LogInformation("Finding image paths...");

    var imagePaths = GetImagePaths(directoryRootPath);

    logger.LogInformation("Found {imagePaths.Count} image paths.", imagePaths.Count);

    InsertImagePathsIntoDatabase(imagePaths, serviceProvider);

    List<string> GetImagePaths(string directoryRootPath)
    {
        List<string> imagePaths = [];

        List<string> imageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"];

        foreach(var directory in Directory.GetDirectories(directoryRootPath, "*", SearchOption.AllDirectories))
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

    void InsertImagePathsIntoDatabase(List<string> filePaths, ServiceProvider serviceProvider)
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

            var images = filePaths.Select(filePath =>
            {
                string name = Path.GetFileName(filePath);

                string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);

                if(nameWithoutExtension.Length <= 2)
                {
                    logger.LogWarning("Skipping file with short name: {imagePath}", filePath);
                    return (IsValid: false, Image: null as Image);
                }

                string unixTimeString = nameWithoutExtension[..^2];

                if(!long.TryParse(unixTimeString, out long unixTime))
                {
                    logger.LogWarning("Skipping file with invalid Unix time: {imagePath}", filePath);
                    return (IsValid: false, Image: null as Image);
                }

                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

                string siteName = siteNames.FirstOrDefault(s => filePath.Contains(s)) ?? string.Empty;

                int camera = filePath.Contains("Camera2") ? 2 : 1;

                var image = new Image
                {
                    Name = name,
                    FilePath = filePath,
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
                //This is ugly, but loggers hate interpolation
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

            int count = context.SaveChanges();

            logger.LogInformation("Insertion complete. {addedCount} insertions.", count);
        }
    }
}
catch(Exception)
{
    // Do something more interesting here
    throw;
}