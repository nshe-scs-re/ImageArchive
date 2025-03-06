[ApiController]
[Route("api/[controller]")]
public class ImageAnalysisController : ControllerBase
{
    private readonly ImageAnalysisService _analysisService;

    public ImageAnalysisController(ImageAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitImage(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var jobId = _analysisService.EnqueueImage(ms.ToArray());
        return Ok(new { jobId });
    }

    [HttpGet("{jobId}")]
    public IActionResult GetResult(string jobId)
    {
        var result = _analysisService.GetResult(jobId);
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}