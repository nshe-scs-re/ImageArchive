using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace InitializeDatabase;
internal class Program
{
    static bool _verboseLoggingEnabled = false;
    static void Main(string[] args)
    {
        _verboseLoggingEnabled = args.Contains("-v") || args.Contains("--verbose");

        LogToConsole("Loading configuration...");
        var configuration = LoadConfiguration();
        LogToConsole("Success!");

        LogToConsole("Loading services...");
        using var serviceProvider = LoadServices(configuration);
        LogToConsole("Success!");

        using(var scope = serviceProvider.CreateScope())
        {
            using var context = scope.ServiceProvider.GetRequiredService<ImageDbContext>();

            LogToConsole("Applying database migrations...");
            context.Database.Migrate();
            LogToConsole("Success!");
        }

        LogToConsole("Getting image directory base path...");
        var imageDirectoryBasePath = GetImageDirectoryBasePath();
        LogToConsole("Success!");

        LogToConsole("Getting image file paths...");
        var imageFilePaths = GetImageFilePaths(imageDirectoryBasePath);
        LogToConsole("Success!");

        LogToConsole("Parsing headers...");
        var parsedHeaders = ParseHeaders(imageFilePaths);
        LogToConsole("Success!");

        LogToConsole("Inserting into database...");
        InsertIntoDatabase(imageFilePaths, parsedHeaders, serviceProvider);
        LogToConsole("Success!");

        LogToConsole("Script complete.");
    }

    static void LogToConsole(string message, LogLevel logLevel = LogLevel.Information, [CallerMemberName] string? caller = null)
    {
        if (!_verboseLoggingEnabled && (logLevel == LogLevel.Debug || logLevel == LogLevel.Trace))
        {
            return;
        }

        string levelTag = logLevel.ToString().ToUpper();
        ConsoleColor originalColor = Console.ForegroundColor;
        ConsoleColor logLevelColor = Console.ForegroundColor;

        switch (logLevel)
        {
            case LogLevel.Debug:
            case LogLevel.Trace:
                logLevelColor = ConsoleColor.Cyan;
                break;
            case LogLevel.Information:
                logLevelColor = ConsoleColor.Green;
                break;
            case LogLevel.Warning:
                logLevelColor = ConsoleColor.Yellow;
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                logLevelColor = ConsoleColor.Red;
                break;
            default:
                logLevelColor = ConsoleColor.White;
                break;
        }

        Console.ForegroundColor = logLevelColor;

        Console.Write($"[{levelTag}] ");

        Console.ForegroundColor = originalColor;

        string timestamp = $"{DateTime.Now:HH:mm:ss}";

        if(message is "Success!")
        {
            Console.Write($"[{caller}] [{timestamp}]: ");

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine(message);

            Console.ForegroundColor = originalColor;
        }
        else
        {
            Console.WriteLine($"[{caller}] [{timestamp}]: {message}");
        }
    }

    static Dictionary<string, int> ParseHeaders(List<string> filePaths)
    {
        var byteMapping = new ConcurrentDictionary<string, int>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        Parallel.ForEach(filePaths, options, filePath =>
        {
            if(!Path.IsPathFullyQualified(filePath))
            {
                LogToConsole("Invalid file path. Skipping file.", logLevel: LogLevel.Debug);
                return;
            }

            if(!File.Exists(filePath))
            {
                LogToConsole("Invalid file path. Skipping file.", logLevel: LogLevel.Debug);
                return;
            }

            using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader binaryReader = new BinaryReader(fileStream);

            if(fileStream.Length < 2)
            {
                LogToConsole($"File too small to be a valid JPEG file. Skipping file at: {filePath}", logLevel: LogLevel.Debug);
                return;
            }

            var firstHeader = ConvertToBigEndian16(binaryReader.ReadUInt16());

            if(firstHeader != 0xFFD8) // JPEG SOI marker (0xFFD8)
            {
                LogToConsole($"Unexpected SOI marker: 0x{firstHeader:X4}. Skipping file: {filePath}", logLevel: LogLevel.Debug);
                return;
            }

            fileStream.Position = fileStream.Length - 2;

            ushort segmentMarker = ConvertToBigEndian16(binaryReader.ReadUInt16());

            if(segmentMarker != 0xFFD9) // JPEG EOI marker (0xFFD9)
            {
                LogToConsole($"Unexpected EOI marker: 0x{segmentMarker:X4}. Skipping file: {filePath}", logLevel: LogLevel.Debug);
                return;
            }

            fileStream.Position = 2;

            while(fileStream.Position < fileStream.Length)
            {
                if(fileStream.Length - fileStream.Position < 4)
                {
                    LogToConsole($"File stream length must be > 4 bytes. Skipping file: {filePath}", logLevel: LogLevel.Debug);
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
                    LogToConsole($"File stream length too short. Skipping file: {filePath}", logLevel: LogLevel.Debug);
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
                    LogToConsole($"APP0 header is too short. Skipping file: {filePath}", logLevel: LogLevel.Debug);
                    break;
                }

                byte byte_27 = app0_header[26]; // zero-based array
                byte byte_29 = app0_header[28]; // zero-based array

                if(byte_27 != byte_29)
                {
                    LogToConsole($"Byte 27 and Byte 29 do not match. Skipping file: {filePath}", logLevel: LogLevel.Debug);
                    break;
                }

                // implicit conversion of byte_27 from byte to int
                if(!byteMapping.TryAdd(filePath, byte_27))
                {
                    LogToConsole($"Could not add image with path to byte mapping: {filePath}", logLevel: LogLevel.Debug);
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
            "Sheep",
            "Snake",
            "Spring",
            "Rockland",
            "Eldorado"
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

        var imageFiles = filePaths.AsParallel().Select(filePath =>
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if(fileName.Length <= 2)
            {
                LogToConsole($"Name is too short. Skipping file: {filePath}", logLevel: LogLevel.Debug);
                return (IsValid: false, Image: null as Image);
            }

            if(!long.TryParse(fileName[..^2], out long unixTime))
            {
                LogToConsole($"Invalid Unix time. Skipping file: {filePath}", logLevel: LogLevel.Debug);
                return (IsValid: false, Image: null as Image);
            }

            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

            var siteName = siteNames.FirstOrDefault(s => filePath.Contains(s, StringComparison.OrdinalIgnoreCase));

            var siteNumberString = siteNumbers.FirstOrDefault(s => filePath.Contains(s, StringComparison.OrdinalIgnoreCase));

            var siteNumber = siteNumberString is null ? -1 : int.Parse(siteNumberString[^1].ToString());

            if(siteNumber == -1)
            {
                LogToConsole($"Variable '{nameof(siteNumber)}' has been assigned an error value as '{nameof(siteNumberString)}' is null. File: {filePath}", logLevel: LogLevel.Debug);
            }

            if(!byteMap.TryGetValue(filePath, out int cameraPositionNumber))
            {
                return (IsValid: false, Image: null as Image);
            }

            string? cameraPositionName = CameraPositionMap.GetCameraPositionName(siteName, siteNumber, cameraPositionNumber);

            var image = new Image
            {
                FilePath = ConfigureFilePath(basePath, filePath),
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
        .OrderBy(image => image!.DateTime)
        .ToList();

        if(imageFiles.Count == 0)
        {
            LogToConsole("No images available. Exiting application...", logLevel: LogLevel.Information);
            return;
        }

        try
        {
            context.Images.AddRange(imageFiles!);
            int count = context.SaveChanges();
            LogToConsole($"Database insertion successful. {count} database entries.", logLevel: LogLevel.Information);
        }
        catch(Exception e)
        {
            LogToConsole($"Exception message: {e.Message}", logLevel: LogLevel.Error);
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
            : "/mnt/nvme0n1/images";
    }

    static string ConfigureFilePath(string basePath, string filePath)
    {
        string targetBasePath = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production"
            ? "/mnt/nvme0n1/images"
            : "/app/images";

        return filePath.Replace(basePath, targetBasePath).Replace('\\', '/');
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

