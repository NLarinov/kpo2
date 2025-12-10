using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FileStoringService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileStoringService.Services;

public interface IFileStorageService
{
    Task<WorkSubmission> StoreFileAsync(IFormFile file, WorkSubmissionRequest request);
    Task<WorkSubmission?> GetSubmissionAsync(Guid id);
    Task<byte[]?> GetFileAsync(Guid id);
    Task<List<WorkSubmission>> GetSubmissionsByAssignmentAsync(string assignmentId);
}

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public FileStorageService(
        FileStorageDbContext context,
        IWebHostEnvironment environment,
        ILogger<FileStorageService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<WorkSubmission> StoreFileAsync(IFormFile file, WorkSubmissionRequest request)
    {
        var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        var fileId = Guid.NewGuid();
        var fileName = $"{fileId}_{file.FileName}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileHash = await CalculateFileHashAsync(filePath);

        var submission = new WorkSubmission
        {
            Id = fileId,
            StudentName = request.StudentName,
            AssignmentId = request.AssignmentId,
            SubmittedAt = DateTime.UtcNow,
            FileName = file.FileName,
            FilePath = filePath,
            FileHash = fileHash
        };

        _context.WorkSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("File stored: {FileId}, Student: {StudentName}, Assignment: {AssignmentId}",
            fileId, request.StudentName, request.AssignmentId);

        // Запускаем анализ асинхронно
        _ = Task.Run(async () => await StartAnalysisAsync(submission));

        return submission;
    }

    private async Task StartAnalysisAsync(WorkSubmission submission)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var analysisServiceUrl = _configuration["AnalysisServiceUrl"] ?? "http://fileanalysisservice/api";
            
            var request = new
            {
                WorkSubmissionId = submission.Id,
                FileHash = submission.FileHash,
                AssignmentId = submission.AssignmentId
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{analysisServiceUrl}/analysis/start", content);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Analysis started for work {WorkId}", submission.Id);
            }
            else
            {
                _logger.LogWarning("Failed to start analysis for work {WorkId}: {StatusCode}", 
                    submission.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting analysis for work {WorkId}", submission.Id);
        }
    }

    public async Task<WorkSubmission?> GetSubmissionAsync(Guid id)
    {
        return await _context.WorkSubmissions.FindAsync(id);
    }

    public async Task<byte[]?> GetFileAsync(Guid id)
    {
        var submission = await _context.WorkSubmissions.FindAsync(id);
        if (submission == null || !File.Exists(submission.FilePath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(submission.FilePath);
    }

    public async Task<List<WorkSubmission>> GetSubmissionsByAssignmentAsync(string assignmentId)
    {
        return await _context.WorkSubmissions
            .Where(s => s.AssignmentId == assignmentId)
            .OrderBy(s => s.SubmittedAt)
            .ToListAsync();
    }

    private async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash);
    }
}

