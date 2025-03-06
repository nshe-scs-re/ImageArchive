using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class ImageAnalysisController : ControllerBase
{
    private readonly ImageAnalysisService _analysisService;
    private readonly ILogger<ImageAnalysisController> _logger;

    public ImageAnalysisController(ImageAnalysisService analysisService, ILogger<ImageAnalysisController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> SubmitImage(IFormFile file)
    {
        try
        {
            _logger.LogInformation("Receiving image upload");
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var jobId = _analysisService.EnqueueImage(ms.ToArray());
            _logger.LogInformation("Image enqueued with job ID: {JobId}", jobId);
            return Ok(new { jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image upload");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{jobId}")]
    public IActionResult GetResult(string jobId)
    {
        try
        {
            var result = _analysisService.GetResult(jobId);
            if (result == null)
                return NotFound(new { message = "Result not ready" });
            
            if (result.Error != null)
                return BadRequest(new { error = result.Error });
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving result for job {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}