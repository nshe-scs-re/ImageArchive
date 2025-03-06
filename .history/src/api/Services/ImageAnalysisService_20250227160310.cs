using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

public class ImageAnalysisService : BackgroundService
{
    private readonly ILogger<ImageAnalysisService> _logger;
    private readonly ConcurrentQueue<ImageAnalysisJob> _queue;
    private readonly ConcurrentDictionary<string, ImageAnalysisResult> _results;
    private readonly string _modelPath;
    private readonly string _scriptPath;

    public ImageAnalysisService(ILogger<ImageAnalysisService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _queue = new ConcurrentQueue<ImageAnalysisJob>();
        _results = new ConcurrentDictionary<string, ImageAnalysisResult>();
        
        // Set paths relative to the application
        _modelPath = Path.Combine(env.ContentRootPath, "predictor", "Local_data_for_model", "best_model.pth");
        _scriptPath = Path.Combine(env.ContentRootPath, "predictor", "Local_data_for_model", "classify_image.py");
    }

    public string EnqueueImage(byte[] imageData)
    {
        var jobId = Guid.NewGuid().ToString();
        _queue.Enqueue(new ImageAnalysisJob
        {
            JobId = jobId,
            ImageData = imageData,
            Timestamp = DateTime.UtcNow
        });
        return jobId;
    }

    public ImageAnalysisResult? GetResult(string jobId)
    {
        _results.TryGetValue(jobId, out var result);
        return result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var job))
            {
                try
                {
                    _logger.LogInformation("Processing job {JobId}", job.JobId);
                    
                    // Save image to temp file
                    var tempPath = Path.Combine(Path.GetTempPath(), $"{job.JobId}.jpg");
                    await File.WriteAllBytesAsync(tempPath, job.ImageData, stoppingToken);

                    // Run Python script
                    var result = await RunPythonScript(tempPath);
                    result.CompletedAt = DateTime.UtcNow;
                    
                    // Store result
                    _results[job.JobId] = result;

                    // Cleanup
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);

                    _logger.LogInformation("Completed job {JobId}", job.JobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing image analysis job {JobId}", job.JobId);
                    // Store error result
                    _results[job.JobId] = new ImageAnalysisResult 
                    { 
                        Error = ex.Message,
                        CompletedAt = DateTime.UtcNow
                    };
                }
            }

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task<ImageAnalysisResult> RunPythonScript(string imagePath)
    {
        var result = new ImageAnalysisResult();

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"\"{_scriptPath}\" \"{imagePath}\" --model \"{_modelPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(TimeSpan.FromSeconds(30));

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new Exception($"Python script failed: {error}");
            }

            // Parse the output
            foreach (var line in output.Split('\n'))
            {
                if (line.StartsWith("Prediction:"))
                    result.Prediction = line.Split(':')[1].Trim();
                else if (line.StartsWith("Snow Probability:"))
                    result.SnowProbability = double.Parse(line.Split(':')[1].Trim().TrimEnd('%')) / 100;
                else if (line.StartsWith("No Snow Probability:"))
                    result.NoSnowProbability = double.Parse(line.Split(':')[1].Trim().TrimEnd('%')) / 100;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Python script for image {ImagePath}", imagePath);
            throw;
        }

        return result;
    }
}

public class ImageAnalysisJob
{
    public string JobId { get; set; } = "";
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; }
}

public class ImageAnalysisResult
{
    public string Prediction { get; set; } = "";
    public double SnowProbability { get; set; }
    public double NoSnowProbability { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? Error { get; set; }
}