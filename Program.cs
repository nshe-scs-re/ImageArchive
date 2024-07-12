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

            string rootPath = @"C:\Sheep Images Test";

            var imagePaths = GetImagePaths(rootPath);

            InsertImagePathsIntoDatabase(imagePaths, serviceProvider);
        }

        static List<string> GetImagePaths(string rootPath)
        {
            var imagePaths = new List<string>();

            foreach (var directory in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                foreach (var file in Directory.GetFiles(directory, "*.jpg"))
                {
                    imagePaths.Add(file);
                }
            }

            return imagePaths;
        }

        static void InsertImagePathsIntoDatabase(List<string> imagePaths, ServiceProvider serviceProvider)
        {
            Console.WriteLine("Images to be inserted into the database:");

            foreach (var imagePath in imagePaths)
            {
                string name = Path.GetFileName(imagePath);
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
                long unixTime = long.Parse(nameWithoutExtension.Substring(0, nameWithoutExtension.Length - 2));
                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

                Console.WriteLine($"Name: {name}");
                Console.WriteLine($"FilePath: {imagePath}");
                Console.WriteLine($"DateTime: {dateTime}");
                Console.WriteLine($"UnixTime: {unixTime}");
                Console.WriteLine();
            }

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

                    return new Image
                    {
                        Name = name,
                        FilePath = imagePath,
                        DateTime = dateTime,
                        UnixTime = unixTime
                    };
                });

                context.Images.AddRange(images);
                context.SaveChanges();
            }
        }
    }
}
