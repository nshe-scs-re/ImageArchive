using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MLController : ControllerBase
    {
        private readonly ILogger<MLController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public MLController(ILogger<MLController> logger, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeImage()
        {
            try
            {
                // Check if there's a file in the request
                if (!Request.Form.Files.Any())
                {
                    return BadRequest("No file uploaded");
                }

                var file = Request.Form.Files[0];
                
                // Save the file to a temporary location
                var tempFileName = $"temp_{Guid.NewGuid()}.jpg";
                var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
                
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Get paths to the Python script and model
                var solutionRoot = Directory.GetParent(_environment.ContentRootPath)?.FullName;
                if (solutionRoot == null)
                {
                    throw new DirectoryNotFoundException("Could not find solution root directory");
                }

                var scriptPath = Path.Combine(solutionRoot, "predictor", "Model", "classify_images.py");
                var modelPath = Path.Combine(solutionRoot, "predictor", "Model", "best_model.pth");

                // Check if the script and model files exist
                if (!System.IO.File.Exists(scriptPath))
                {
                    throw new FileNotFoundException($"Python script not found at {scriptPath}");
                }
                
                if (!System.IO.File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"Model file not found at {modelPath}");
                }

                // Run the Python script
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python3",
                        Arguments = $"\"{scriptPath}\" \"{tempPath}\" --model \"{modelPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Clean up the temporary file
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }

                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Python script failed: {error}");
                    return StatusCode(500, $"Python script failed: {error}");
                }

                // Parse the output
                var result = new ClassificationResult();
                foreach (var line in output.Split('\n'))
                {
                    if (line.StartsWith("Prediction:"))
                        result.Prediction = line.Split(':')[1].Trim();
                    else if (line.StartsWith("Snow Probability:"))
                        result.SnowProbability = double.Parse(line.Split(':')[1].Trim().TrimEnd('%')) / 100;
                    else if (line.StartsWith("No Snow Probability:"))
                        result.NoSnowProbability = double.Parse(line.Split(':')[1].Trim().TrimEnd('%')) / 100;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image");
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class ClassificationResult
    {
        public string Prediction { get; set; } = "";
        public double SnowProbability { get; set; }
        public double NoSnowProbability { get; set; }
    }
} 