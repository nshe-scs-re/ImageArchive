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

        using var serviceProvider = LoadServices(configuration);

        using(var scope = serviceProvider.CreateScope())
        {
            using var context = scope.ServiceProvider.GetRequiredService<ImageDbContext>();
            context.Database.Migrate();
        }

        var imageDirectoryBasePath = GetImageDirectoryBasePath();

        var imageFilePaths = GetImageFilePaths(imageDirectoryBasePath);

        InsertIntoDatabase(imageFilePaths, ParseHeaders(imageFilePaths), serviceProvider);
    }

    static Dictionary<string, int> ParseHeaders(List<string> filePaths)
    {
        var byteMapping = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(filePaths, filePath =>
        {
            if(!Path.IsPathFullyQualified(filePath))
            {
                Console.WriteLine("[WARNING] [Program.cs] [ParseHeaders]: Invalid file path. Skipping file.");
                return;
            }

            if(!File.Exists(filePath))
            {
                Console.WriteLine("[WARNING] [Program.cs] [ParseHeaders]: Invalid file path. Skipping file.");
                return;
            }

            using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader binaryReader = new BinaryReader(fileStream);

            if(fileStream.Length < 2)
            {
                Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: File too small to be a valid JPEG file. Skipping file at path '{filePath}'");
                return;
            }

            var firstHeader = ConvertToBigEndian16(binaryReader.ReadUInt16());

            if(firstHeader != 0xFFD8) // JPEG SOI marker (0xFFD8)
            {
                Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: Unexpected SOI marker: 0x{firstHeader:X4}. Skipping file at path '{filePath}'");
                return;
            }

            fileStream.Position = fileStream.Length - 2;

            ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

            if(segmentMarker != 0xFFD9) // JPEG EOI marker (0xFFD9)
            {
                Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: Unexpected EOI marker: 0x{segmentMarker:X4}. Skipping file at path '{filePath}'");
                return;
            }

            fileStream.Position = 2;

            while(fileStream.Position < fileStream.Length)
            {
                if(fileStream.Length - fileStream.Position < 4)
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: File stream length must be > 4 bytes. Skipping file at path '{filePath}'");
                    break; // Length must be at least 4 bytes
                }

                segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16()); // 2 bytes
                ushort segmentLength = ConvertToBigEndian16(binaryReader.ReadUInt16()); // 2 bytes minimum

                if(segmentLength < 2)
                {
                    continue;
                }

                if(fileStream.Length - fileStream.Position < segmentLength - 2)
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: File stream length too short. Skipping file at path '{filePath}'");
                    break; // Avoid reading passed EOF
                }

                // APP0 marker 0xFFE0
                if(segmentMarker != 0xFFE0)
                {
                    fileStream.Position += (segmentLength - 2);
                    continue;
                }

                byte[] app0_header = binaryReader.ReadBytes(segmentLength - 2);

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

                // implicit conversion of byte_27 from byte to int
                if(!byteMapping.TryAdd(filePath, byte_27))
                {
                    Console.WriteLine($"[WARNING] [Program.cs] [ParseHeaders]: Could not add image with path '{filePath}' to byte mapping.");
                }

                break; // Move to next file after reading APP0 header
            }
        });

        return new Dictionary<string, int>(byteMapping);
    }

    static ushort ConvertToBigEndian16(ushort value)
    {
        ushort originalMsb = (ushort)((value >> 8) & 0xFF);
        ushort originalLsb = (ushort)(value & 0xFF);

        ushort newMsb = (ushort)(originalLsb << 8);
        ushort newLsb = originalMsb;

        return (ushort)(newMsb | newLsb);
    }

    static void InsertIntoDatabase(List<string> filePaths, Dictionary<string, int> byteMap, ServiceProvider serviceProvider)
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

        var basePath = GetImageDirectoryBasePath();

        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<ImageDbContext>();

        var imageFiles = filePaths.Select(filePath =>
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if(fileName.Length <= 2)
            {
                Console.WriteLine($"[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: Name is too short. Skipping file at path '{filePath}'");
                return (IsValid: false, Image: null as Image);
            }

            if(!long.TryParse(fileName[..^2], out long unixTime))
            {
                Console.WriteLine($"[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: Invalid Unix time. Skipping file at path '{filePath}'");
                return (IsValid: false, Image: null as Image);
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

            var siteName = siteNames.FirstOrDefault(s => filePath.Contains(s, StringComparison.OrdinalIgnoreCase));

            var siteNumberString = siteNumbers.FirstOrDefault(s => filePath.Contains(s, StringComparison.OrdinalIgnoreCase));

            var siteNumber = siteNumberString is null ? -1 : int.Parse(siteNumberString[^1].ToString());

            if(siteNumber == -1)
            {
                Console.WriteLine($"[WARNING] [Program.cs] [InsertIntoDatabase]: Variable '{nameof(siteNumber)}' has been assigned an error value as '{nameof(siteNumberString)}' is null.");
            }

            if(!byteMap.TryGetValue(filePath, out int cameraPositionNumber))
            {
                return (IsValid: false, Image: null as Image);
            }

            string? cameraPositionName = CameraPositionMap.GetCameraPositionName(siteName, siteNumber, cameraPositionNumber);

            var image = new Image
            {
                FilePath = ConvertSingleWindowsPathToLinuxPath(basePath, filePath),
                UnixTime = unixTime,
                DateTime = dateTime,
                SiteName = siteName,
                SiteNumber = siteNumber,
                CameraPositionNumber = cameraPositionNumber,
                CameraPositionName = cameraPositionName
            };

            return (IsValid: true, Image: image);

        })
        .Where(result => result.IsValid)
        .Select(result => result.Image)
        .ToList();

        if(imageFiles.Count == 0)
        {
            Console.WriteLine("[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: No images available. Exiting application...");
            return;
        }

        try
        {
            context.Images.AddRange(imageFiles!);
            int count = context.SaveChanges();
            Console.WriteLine($"[INFO] [InitializeDatabase] [InsertImagePathsIntoDatabase]: Database insertion complete. {count} database entries.");
        }
        catch(Exception)
        {
            throw;
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

    static string ConvertSingleWindowsPathToLinuxPath(string basePath, string filePath)
    {
        string linuxBasePath = "/app/images";

        if(basePath == linuxBasePath)
        {
            return filePath; 
        }

        return filePath.Replace(basePath, linuxBasePath).Replace('\\', '/');
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
}

