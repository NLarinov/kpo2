using FileStoringService.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace FileStoringService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileStorageController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FileStorageController> _logger;

    public FileStorageController(
        IFileStorageService fileStorageService,
        ILogger<FileStorageController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    [HttpPost("submit")]
    [ProducesResponseType(typeof(WorkSubmission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitWork(
        [FromForm] IFormFile file,
        [FromForm] string studentName,
        [FromForm] string assignmentId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(assignmentId))
        {
            return BadRequest("Student name and assignment ID are required");
        }

        try
        {
            var request = new WorkSubmissionRequest
            {
                StudentName = studentName,
                AssignmentId = assignmentId
            };

            var submission = await _fileStorageService.StoreFileAsync(file, request);
            return Ok(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing file");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkSubmission), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmission(Guid id)
    {
        var submission = await _fileStorageService.GetSubmissionAsync(id);
        if (submission == null)
        {
            return NotFound();
        }

        return Ok(submission);
    }

    [HttpGet("{id}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var submission = await _fileStorageService.GetSubmissionAsync(id);
        if (submission == null)
        {
            return NotFound();
        }

        var fileBytes = await _fileStorageService.GetFileAsync(id);
        if (fileBytes == null)
        {
            return NotFound();
        }

        return File(fileBytes, "application/octet-stream", submission.FileName);
    }

    [HttpGet("assignment/{assignmentId}")]
    [ProducesResponseType(typeof(List<WorkSubmission>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissionsByAssignment(string assignmentId)
    {
        var submissions = await _fileStorageService.GetSubmissionsByAssignmentAsync(assignmentId);
        return Ok(submissions);
    }
}

