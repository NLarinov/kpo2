using FileAnalysisService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace FileAnalysisService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly IWordCloudService _wordCloudService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        IAnalysisService analysisService,
        IWordCloudService wordCloudService,
        ILogger<AnalysisController> logger)
    {
        _analysisService = analysisService;
        _wordCloudService = wordCloudService;
        _logger = logger;
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(AnalysisReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartAnalysis([FromBody] StartAnalysisRequest request)
    {
        if (request == null || request.WorkSubmissionId == Guid.Empty)
        {
            return BadRequest("Work submission ID is required");
        }

        try
        {
            var report = await _analysisService.StartAnalysisAsync(
                request.WorkSubmissionId,
                request.FileHash ?? string.Empty,
                request.AssignmentId ?? string.Empty);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting analysis");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("report/{reportId}")]
    [ProducesResponseType(typeof(AnalysisReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(Guid reportId)
    {
        var report = await _analysisService.GetReportAsync(reportId);
        if (report == null)
        {
            return NotFound();
        }

        return Ok(report);
    }

    [HttpGet("works/{workId}/reports")]
    [ProducesResponseType(typeof(WorkReportsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkReports(Guid workId)
    {
        var response = await _analysisService.GetWorkReportsAsync(workId);
        return Ok(response);
    }

    [HttpGet("report/{reportId}/wordcloud")]
    [ProducesResponseType(typeof(WordCloudResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWordCloud(Guid reportId)
    {
        var report = await _analysisService.GetReportAsync(reportId);
        if (report == null)
        {
            return NotFound();
        }

        if (report.WordFrequency == null || report.WordFrequency.Count == 0)
        {
            return NotFound("Word frequency data not available");
        }

        var wordCloudUrl = await _wordCloudService.GenerateWordCloudUrlAsync(report.WordFrequency);
        
        return Ok(new WordCloudResponse
        {
            ReportId = reportId,
            WordCloudUrl = wordCloudUrl
        });
    }
}

public class StartAnalysisRequest
{
    public Guid WorkSubmissionId { get; set; }
    public string? FileHash { get; set; }
    public string? AssignmentId { get; set; }
}

public class WordCloudResponse
{
    public Guid ReportId { get; set; }
    public string WordCloudUrl { get; set; } = string.Empty;
}

