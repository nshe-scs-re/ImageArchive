using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace InitializeDatabase;
internal class Program
{
    static void Main(string[] args)
    {
        var configuration = LoadConfiguration();

        using var serviceProvider = LoadServices(configuration);

        var imageDirectoryBasePath = GetImageDirectoryBasePath();

        var imageFilePaths = GetImageFilePaths(imageDirectoryBasePath);

        InsertIntoDatabase(imageFilePaths, serviceProvider);
    }

    static ServiceProvider LoadServices(IConfigurationRoot configuration)
    {
        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddDbContext<ImageDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("ImageDb"));
            })
            .BuildServiceProvider();
    }

    static IConfigurationRoot LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
    }

    static string GetImageDirectoryBasePath()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "images")
            : "/app/images";
    }

    static List<string> GetImageFilePaths(string basePath)
    {
        var extensions = new List<string> { ".jpg", ".jpeg" };
        var files = new ConcurrentBag<string>();

        Parallel.ForEach(Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories), file =>
        {
            if(extensions.Contains(Path.GetExtension(file).ToLower()))
            {
                files.Add(file);
            }
        });

        return files.ToList();
    }

    static void InsertIntoDatabase(List<string> filePaths, ServiceProvider serviceProvider)
    {
        List<string> siteNames = new List<string>
        {
            "sheep",
            "snake",
            "spring",
            "rockland",
            "eldorado"
        };

        List<string> siteNumbers = new List<string>
        {
            "site_0",
            "site_1",
            "site_2",
            "site_3",
            "site_4",
            "site_5",
        };

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ImageDbContext>();

        var imageFiles = filePaths.Select(filePath =>
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if(fileName.Length <= 2)
            {
                Console.WriteLine($"[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: Skipping file with short name: {filePath}");
                return (IsValid: false, Image: null as Image);
            }

            if(!long.TryParse(fileName[..^2], out long unixTime))
            {
                Console.WriteLine($"[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: Skipping file with invalid Unix time: {filePath}");
                return (IsValid: false, Image: null as Image);
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

            var siteName = siteNames.FirstOrDefault(s => filePath.Contains(s, StringComparison.OrdinalIgnoreCase));

            var siteNumberString = siteNumbers.FirstOrDefault(s => filePath.Contains(s, StringComparison.OrdinalIgnoreCase));

            var siteNumberInteger = siteNumberString is null ? 1 : int.Parse(siteNumberString[^1].ToString());

            var image = new Image
            {
                FilePath = filePath,
                UnixTime = unixTime,
                DateTime = dateTime,
                SiteName = siteName,
                SiteNumber = siteNumberInteger,
            };

            return (IsValid: true, Image: image);

        })
        .Where(result => result.IsValid)
        .Select(result => result.Image)
        .ToList();

        if(imageFiles.Count == 0)
        {
            Console.WriteLine("[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: No images available. Exiting...");
            return;
        }

        try
        {
            context.Images.AddRange(imageFiles!);
            int count = context.SaveChanges();
            Console.WriteLine($"[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: Database insertion complete. {count} entries.");
        }
        catch(Exception)
        {
            throw;
        }
    }
}
