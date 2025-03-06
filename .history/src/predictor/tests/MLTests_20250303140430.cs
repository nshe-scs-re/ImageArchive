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
            var scriptPath = Path.Combine("..", "..", "..", "..", "predictor", "Local_data_for_model", "classify_image.py");

            // Act
            bool scriptExists = File.Exists(scriptPath);

            // Assert
            Assert.True(scriptExists, "The Python script should exist at the specified path");
        }
    }
}