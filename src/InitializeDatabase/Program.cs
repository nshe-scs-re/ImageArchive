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

    static void ParseHeaders(List<string> filePaths)
    {
        var byteMapping = new Dictionary<string, int>();

        foreach(var filePath in filePaths)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var binaryRead = new BinaryReader(fileStream);

            while(fileStream.Position < fileStream.Length)
            {
                if(fileStream.Length < (fileStream.Position + 4))
                {
                    break; // Length must be at least 4 bytes
                }

                ushort segmentMarker = ConvertToBigEndian16(binaryRead.ReadUInt16()); // 2 bytes
                ushort segmentLength = ConvertToBigEndian16(binaryRead.ReadUInt16()); // 2 bytes minimum

                if(segmentLength < 2)
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: Skipping file at path '{filePath}'");
                    break; // Invalid segment length
                }

                if(fileStream.Length < (fileStream.Position + segmentLength - 2))
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: Skipping file at path '{filePath}'");
                    break; // Avoid reading passed EOF
                }

                // APP0 marker (0xFFE0)
                if(segmentMarker != 0xFFE0)
                {
                    fileStream.Position += (segmentLength - 2);
                    continue;
                }
                
                byte[] app0_header = binaryRead.ReadBytes(segmentLength - 2);

                if(app0_header.Length < 29)
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: APP0 header is too short. Skipping file at path '{filePath}'");
                    break;
                }

                byte byte_27 = app0_header[26]; // zero-based array
                byte byte_29 = app0_header[28]; // zero-based array

                if(byte_27 != byte_29)
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: Byte 27 and Byte 29 do not match. Skipping file at path '{filePath}'");
                    break;
                }

                int byte_27_decimal_value = byte_27;

                if(!byteMapping.TryAdd(filePath, byte_27_decimal_value))
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: Could not add image with path '{filePath}' to byte mapping.");
                }
            }
        }
    }

    static ushort ConvertToBigEndian16(ushort value)
    {
        ushort originalMsb = (ushort)((value >> 8) & 0xFF);
        ushort originalLsb = (ushort)(value & 0xFF);

        ushort newMsb = (ushort)(originalLsb << 8);
        ushort newLsb = originalMsb;

        return (ushort)(newMsb | newLsb);
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
