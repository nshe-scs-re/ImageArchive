using ExtractImagePaths.Data;
using ExtractImagePaths.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

string user = "whaley";

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

var windowsUserName = configuration["WINDOWS_USER_NAME"] ?? configuration["WindowsUser:Name"];
var windowsBasePath = configuration["WINDOWS_USER_BASE_PATH"] ?? configuration["WindowsUser:BasePath"];
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
    .BuildServiceProvider();

Console.WriteLine($"[INFO] [Program.cs] [Main]: Environment is set to: {configuration["ASPNETCORE_ENVIRONMENT"]}.");
Console.WriteLine($"[INFO] [Program.cs] [Main]: Directory root path is set to: {directoryRootPath}.");

using(var scope = serviceProvider.CreateScope())
{
    var context = scope.ServiceProvider.GetService<ImageDbContext>();

    if(context.Database.CanConnect())
    {
        Console.WriteLine($"[INFO] [Program.cs] [Main]: Database connection success!");
    }
    else
    {
        Console.WriteLine("[ERROR] [Program.cs] [Main]: Could not connect to database. Exiting application.");
        return;
    }
}

try
{
    Console.WriteLine($"[INFO] [Program.cs] [Main]: Root directory is set to: {directoryRootPath}");

    if(Directory.Exists(directoryRootPath))
    {
        Console.WriteLine($"[INFO] [Program.cs] [Main]: Root directory exists. Finding subdirectories...");

        string[] directories = Directory.GetDirectories(directoryRootPath);

        Console.WriteLine($"[INFO] [Program.cs] [Main]: Found {directories.Length} subdirectories:\n\t{string.Join("\n\t", directories)}");
    }
    else
    {
        Console.WriteLine($"[ERROR] [Program.cs] [Main]: {directoryRootPath} does not exist. Exiting.", directoryRootPath);
        return;
    }
}
catch(Exception ex)
{
    Console.WriteLine("{ex.Message}", ex.Message);
    return;
}

Console.WriteLine($"[INFO] [Program.cs] [Main]: Finding image paths...");

var imagePaths = GetImagePaths(directoryRootPath);

Console.WriteLine($"[INFO] [Program.cs] [Main]: Found {imagePaths.Count} image paths.", imagePaths.Count);

if(imagePaths.Count is 0)
{
    Console.WriteLine("[INFO] [Program.cs] [Main]: No image paths found. Exiting application.");
    return;
}

InsertImagePathsIntoDatabase(imagePaths, serviceProvider);

List<string> GetImagePaths(string directoryRootPath)
{
    List<string> imagePaths = new();

    List<string> imageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"];

    Directory.EnumerateFiles(directoryRootPath, "*.*", SearchOption.AllDirectories)
        .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
        .ToList()
        .ForEach(imagePaths.Add);

    return imagePaths;
}

void InsertImagePathsIntoDatabase(List<string> filePaths, ServiceProvider serviceProvider)
{
    List<string> siteNames = new List<string>
    {
        "sheep",
        "snake",
        "sagehen",
        "spring",
        "conness",
        "rockland",
        "lassen",
        "eldorado"
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
                Console.WriteLine($"[INFO] [Program.cs] [Main]: Skipping file with short name: {filePath}");
                return (IsValid: false, Image: null as Image);
            }

            string unixTimeString = nameWithoutExtension[..^2];

            if(!long.TryParse(unixTimeString, out long unixTime))
            {
                Console.WriteLine($"[INFO] [Program.cs] [Main]: Skipping file with invalid Unix time: {filePath}");
                return (IsValid: false, Image: null as Image);
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

            string siteName = siteNames.FirstOrDefault(s => filePath.Contains(s)) ?? string.Empty;

            int siteNumber = 1;

            for(int i = 0; i < 10; i++)
            {
                string s = $"site_{i}";
                if(filePath.Contains(s))
                {
                    siteNumber = i;
                }
            }

            int Number = filePath.Contains("Camera2") ? 2 : 1; //TODO: Potentially remove

            if(filePath.Contains('\\'))
            {
                if(!string.IsNullOrEmpty(windowsBasePath))
                {
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
                CameraNumber = Number
            };

            return (IsValid: true, Image: image);

        })
        .Where(result => result.IsValid)
        .Select(result => result.Image)
        .ToList();

        if(images.Count != 0)
        {
            Console.WriteLine($"""
                "Example insertion:"
                "Name: {images[0]!.Name}"
                "File Path: {images[0]!.FilePath}"
                "Unix Time: {images[0]!.UnixTime}"
                "DateTime: {images[0]!.DateTime}"
                "SiteName: {images[0]!.SiteName}"
                "SiteNumber: {images[0]!.SiteNumber}"
                "CameraNumber: {images[0]!.CameraNumber}"
                "Camera Position: {images[0]!.CameraPositionNumber}"
            """);
        }
        else
        {
            Console.WriteLine("[INFO] [Program.cs] [Main]: No images available. Exiting...");
            return;
        }

        Console.WriteLine("\nContinue with insertion? (Y/N):");

        string choice = Console.ReadLine()!.Trim().ToUpper();

        while(choice is not "Y" and not "N")
        {
            Console.WriteLine("Invalid input. Please enter Y or N:");
            choice = Console.ReadLine()!.Trim().ToUpper();
        }

        if(choice == "N")
        {
            Console.WriteLine($"Exiting...");
            return;
        }

        context.Images.AddRange(images!);

        int count = context.SaveChanges();

        Console.WriteLine($"[INFO] [Program.cs] [Main]: Insertion complete. {count} insertions.");
    }
}