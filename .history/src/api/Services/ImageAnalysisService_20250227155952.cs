public class ImageAnalysisService : BackgroundService
{
    private readonly ILogger<ImageAnalysisService> _logger;
    private readonly ConcurrentQueue<ImageAnalysisJob> _queue;
    private readonly ConcurrentDictionary<string, ImageAnalysisResult> _results;

    public ImageAnalysisService(ILogger<ImageAnalysisService> logger)
    {
        _logger = logger;
        _queue = new ConcurrentQueue<ImageAnalysisJob>();
        _results = new ConcurrentDictionary<string, ImageAnalysisResult>();
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
                    // Save image to temp file
                    var tempPath = Path.Combine(Path.GetTempPath(), $"{job.JobId}.jpg");
                    await File.WriteAllBytesAsync(tempPath, job.ImageData, stoppingToken);

                    // Run Python script
                    var result = await RunPythonScript(tempPath);

                    // Store result
                    _results[job.JobId] = result;

                    // Cleanup
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing image analysis job {JobId}", job.JobId);
                }
            }

            await Task.Delay(100, stoppingToken); // Small delay to prevent CPU spinning
        }
    }

    private async Task<ImageAnalysisResult> RunPythonScript(string imagePath)
    {
        // Similar to existing Python script execution code
        // but returns ImageAnalysisResult instead of parsing output
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
}