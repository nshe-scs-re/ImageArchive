using ExtractImagePaths.Data;
using ExtractImagePaths.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetEnv;

string user = "";

string envFilePath = $"C:\\Users\\{user}\\source\\secrets\\ExtractImagePaths.env";

if(!File.Exists(envFilePath))
{
    Console.WriteLine($"[ERROR] [Program.cs] [Main]: Environment file not found at {envFilePath}. Exiting.");
    return;
}

DotNetEnv.Env.Load(envFilePath);

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var directoryRootPath = configuration["NEVCAN_DIRECTORY_ROOT_PATH"];

if(string.IsNullOrEmpty(directoryRootPath))
{
    Console.WriteLine($"[ERROR] [Program.cs] [Main]: Variable '{nameof(directoryRootPath)}' not set. Exiting.");
    return;
}

directoryRootPath = Environment.ExpandEnvironmentVariables(directoryRootPath);

var dbConnectionString = configuration["DB_CONNECTION_STRING"] ?? configuration["ConnectionStrings:ImageDb"];

if(string.IsNullOrEmpty(dbConnectionString))
{
    Console.WriteLine($"[ERROR] [Program.cs] [Main]: Variable '{nameof(dbConnectionString)}' not set. Exiting.");
    return;
}

var windowsUserName =  configuration["WINDOWS_USER_NAME"] ?? configuration["WindowsUser:Name"] ;
var windowsBasePath =  configuration["WINDOWS_USER_BASE_PATH"] ?? configuration["WindowsUser:BasePath"];
if(windowsUserName is not null)
{
    windowsUserName = Environment.ExpandEnvironmentVariables(windowsUserName);
}
else
{
    Console.WriteLine($"[WARNING] [Program.cs] [Main]: Variable '{nameof(windowsUserName)}' not set.");
}

if(windowsBasePath is not null)
{
    windowsBasePath = Environment.ExpandEnvironmentVariables(windowsBasePath);
}
else
{
    Console.WriteLine($"[WARNING] [Program.cs] [Main]: Variable '{nameof(windowsBasePath)}' not set.");
}

var serviceProvider = new ServiceCollection()
    .AddSqlServer<ImageDbContext>(dbConnectionString)
    .AddLogging(configure =>
    {
        configure.AddConsole();
        configure.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    })
    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information)
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<Program>>();

logger.LogInformation("Environment is set to: {environment}.", configuration["ASPNETCORE_ENVIRONMENT"]);
logger.LogInformation("Directory root path is set to: {directoryRootPath}.", directoryRootPath);

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

if(imagePaths.Count is 0)
{
    logger.LogError("No image paths found. Exiting application.");
    return;
}

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

            int siteNumber = 1;

            for(int i=0; i<10; i++)
            {
                string s = $"Site {i}";
                if(filePath.Contains(s))
                {
                    siteNumber = i;
                }
            }

            int camera = filePath.Contains("Camera2") ? 2 : 1;

            if(filePath.Contains('\\'))
            {
                if(string.IsNullOrEmpty(windowsBasePath))
                {
                    logger.LogError($"Windows file path found. Variable '{nameof(windowsBasePath)}' is not set. Cannot convert the file path to Linux format.");
                }
                else
                {
                    logger.LogDebug("Windows file path found. Converting Windows file path to Linux file path.");
                    filePath = filePath.Replace(windowsBasePath, "/app/");
                    filePath = filePath.Replace('\\', '/');
                }
            }

            var image = new Image
            {
                Name = name,
                FilePath = filePath,
                DateTime = dateTime,
                UnixTime = unixTime,
                SiteName = siteName,
                SiteNumber = siteNumber,
                Camera = camera
            };

            return (IsValid: true, Image: image);

        })
        .Where(result => result.IsValid)
        .Select(result => result.Image)
        .ToList();

        if(images.Count != 0)
        {
            logger.LogInformation(
                "Example insertion:\n\t" +
                "Name: {Name}\n\t" +
                "File Path: {FilePath}\n\t" +
                "Unix Time: {UnixTime}\n\t" +
                "DateTime: {DateTime}\n\t" +
                "SiteName: {SiteName}\n\t" +
                "SiteNumber: {SiteNumber}\n\t" +
                "Camera: {Camera}\n\t" +
                "Camera Position: {CameraPosition}",
                images[0]!.Name,
                images[0]!.FilePath,
                images[0]!.UnixTime,
                images[0]!.DateTime,
                images[0]!.SiteName,
                images[0]!.SiteNumber,
                images[0]!.Camera,
                images[0]!.CameraPosition
            );
        }
        else
        {
            logger.LogError("No images available. Exiting...");
            return;
        }

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