using ExtractImagePaths.Data;
using ExtractImagePaths.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractImagePaths
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddSqlServer<ImageDbContext>(configuration.GetConnectionString("ImageDatabase"))
                .BuildServiceProvider();

            string rootPath = @"C:\ImagesTest";

            var imagePaths = GetImagePaths(rootPath);

            InsertImagePathsIntoDatabase(imagePaths, serviceProvider);
        }

        static List<string> GetImagePaths(string rootPath)
        {
            List<string> imagePaths = [];

            List<string> imageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff"];

            foreach (var directory in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                foreach(var extension in  imageExtensions)
                {
                    foreach (var file in Directory.GetFiles(directory, $"*{extension}"))
                    {
                        imagePaths.Add(file);
                    }
                }
            }

            return imagePaths;
        }

        static void InsertImagePathsIntoDatabase(List<string> imagePaths, ServiceProvider serviceProvider)
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

            Console.WriteLine("Images to be inserted into the database:");

            foreach (var imagePath in imagePaths)
            {
                string name = Path.GetFileName(imagePath);
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
                long unixTime = long.Parse(nameWithoutExtension.Substring(0, nameWithoutExtension.Length - 2));
                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                string siteName = string.Empty;

                foreach (var s in siteNames)
                {
                    if (imagePath.Contains(s))
                    {
                        siteName = s;
                        break;
                    }
                }

                Console.WriteLine($"File Name: {name}");
                Console.WriteLine($"File Path: {imagePath}");
                Console.WriteLine($"Date Time:  {dateTime}");
                Console.WriteLine($"Unix Time: {unixTime}");
                Console.WriteLine($"Site Name: {siteName}");
                Console.WriteLine();
            }

            Console.WriteLine($"DEBUG: Total count: {imagePaths.Count}");

            Console.WriteLine("Continue with insertion? Y/N ");
            string choice = Console.ReadLine()!.Trim().ToUpper();

            while (choice != "Y" && choice != "N")
            {
                Console.WriteLine("Invalid input. Please enter Y or N.");
                choice = Console.ReadLine()!;
            }
            if (choice == "N")
            {
                Console.WriteLine("Exiting...");
                return;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ImageDbContext>();

                var images = imagePaths.Select(imagePath =>
                {
                    string name = Path.GetFileName(imagePath);
                    string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
                    long unixTime = long.Parse(nameWithoutExtension.Substring(0, nameWithoutExtension.Length - 2));
                    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                    string? siteName = null;
                    int camera = 1;

                    foreach (var s in siteNames)
                    {
                        if (imagePath.Contains(s))
                        {
                            siteName = s;
                            break;
                        }
                    }

                    if(imagePath.Contains("Camera2"))
                    {
                        camera = 2;
                    }

                    return new Image
                    {
                        Name = name,
                        FilePath = imagePath,
                        DateTime = dateTime,
                        UnixTime = unixTime,
                        Site = siteName,
                        Camera = camera
                    };
                });

                context.Images.AddRange(images);
                context.SaveChanges();
            }
        }
    }
}
