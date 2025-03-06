using Xunit;
using System.IO;

namespace SnowPrediction.Tests
{
    public class MLTests
    {
        [Fact]
        public void PythonScript_ShouldExist()
        {
            // Arrange
            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.FullName;
            var scriptPath = Path.Combine(projectRoot!, "Local_data_for_model", "classify_image.py");
            
            Console.WriteLine($"Current Directory: {currentDir}");
            Console.WriteLine($"Looking for script at: {scriptPath}");

            // Act
            bool scriptExists = File.Exists(scriptPath);

            // Assert
            Assert.True(scriptExists, "The Python script should exist at the specified path");
        }
    }
}