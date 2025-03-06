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
            var scriptPath = Path.Combine("..", "src", "predictor", "Local_data_for_model", "classify_image.py");
            Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"Looking for script at: {Path.GetFullPath(scriptPath)}");

            // Act
            bool scriptExists = File.Exists(scriptPath);

            // Assert
            Assert.True(scriptExists, "The Python script should exist at the specified path");
        }
    }
}