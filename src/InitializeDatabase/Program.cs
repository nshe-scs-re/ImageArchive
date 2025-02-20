using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace InitializeDatabase;
internal class Program
{
    static void Main(string[] args)
    {
        var configuration = LoadConfiguration();

        using var services = LoadServices(configuration);

        var imageDirectoryBasePath = GetImageDirectoryBasePath();

        var imageFilePaths = GetImageFilePaths(imageDirectoryBasePath);

        using(var scope = services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ImageDbContext>();
        }
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

        Parallel.ForEach(Directory.GetFiles(basePath, "*", SearchOption.AllDirectories), file =>
        {
            if(extensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
            {
                files.Add(file);
            }
        });

        return files.ToList();
    }
}
