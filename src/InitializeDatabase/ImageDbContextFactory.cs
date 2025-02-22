using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
namespace InitializeDatabase;

public class ImageDbContextFactory : IDesignTimeDbContextFactory<ImageDbContext>
{
    public ImageDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ImageDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("ImageDb"));

        return new ImageDbContext(optionsBuilder.Options);
    }
}
