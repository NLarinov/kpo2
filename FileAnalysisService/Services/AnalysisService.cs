using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FileAnalysisService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models;

namespace FileAnalysisService.Services;

public interface IAnalysisService
{
    Task<AnalysisReport> StartAnalysisAsync(Guid workSubmissionId, string fileHash, string assignmentId);
    Task<AnalysisReport?> GetReportAsync(Guid reportId);
    Task<List<AnalysisReport>> GetReportsByWorkIdAsync(Guid workId);
    Task<WorkReportsResponse> GetWorkReportsAsync(Guid workId);
}

public class AnalysisService : IAnalysisService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnalysisService> _logger;
    private readonly IConfiguration _configuration;

    public AnalysisService(
        IServiceScopeFactory serviceScopeFactory,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        ILogger<AnalysisService> logger,
        IConfiguration configuration)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AnalysisReport> StartAnalysisAsync(Guid workSubmissionId, string fileHash, string assignmentId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        
        var report = new AnalysisReport
        {
            Id = Guid.NewGuid(),
            WorkSubmissionId = workSubmissionId,
            Status = "Pending",
            HasPlagiarism = false,
            CreatedAt = DateTime.UtcNow
        };

        context.AnalysisReports.Add(report);
        await context.SaveChangesAsync();

        var reportId = report.Id;
        var reportWorkSubmissionId = report.WorkSubmissionId;
        var reportCreatedAt = report.CreatedAt;

        _ = Task.Run(async () => await PerformAnalysisAsync(reportId, reportWorkSubmissionId, reportCreatedAt, fileHash, assignmentId));

        return report;
    }

    private async Task PerformAnalysisAsync(Guid reportId, Guid workSubmissionId, DateTime reportCreatedAt, string fileHash, string assignmentId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        
        try
        {
            var report = await context.AnalysisReports.FindAsync(reportId);
            if (report == null)
            {
                _logger.LogWarning("Report {ReportId} not found", reportId);
                return;
            }

            report.Status = "Processing";
            await context.SaveChangesAsync();

            var earlierSubmissions = await context.AnalysisReports
                .Where(r => r.WorkSubmissionId != workSubmissionId
                    && r.CreatedAt < reportCreatedAt
                    && r.Status == "Completed")
                .ToListAsync();

            var fileContent = await GetFileContentAsync(workSubmissionId);
            
            if (fileContent != null)
            {
                var wordFrequency = AnalyzeText(fileContent);
                report.WordFrequency = wordFrequency;

                var plagiarismCheck = await CheckPlagiarismAsync(fileHash, assignmentId, workSubmissionId);
                report.HasPlagiarism = plagiarismCheck.HasPlagiarism;
                report.PlagiarismDetails = plagiarismCheck.Details;
            }

            var reportsPath = Path.Combine(_environment.ContentRootPath, "reports");
            if (!Directory.Exists(reportsPath))
            {
                Directory.CreateDirectory(reportsPath);
            }

            var reportFilePath = Path.Combine(reportsPath, $"{report.Id}.json");
            var reportData = new
            {
                ReportId = report.Id,
                WorkSubmissionId = report.WorkSubmissionId,
                Status = "Completed",
                HasPlagiarism = report.HasPlagiarism,
                PlagiarismDetails = report.PlagiarismDetails,
                WordFrequency = report.WordFrequency,
                CreatedAt = report.CreatedAt,
                CompletedAt = DateTime.UtcNow
            };

            await File.WriteAllTextAsync(reportFilePath, JsonSerializer.Serialize(reportData, new JsonSerializerOptions { WriteIndented = true }));
            report.ReportFilePath = reportFilePath;

            report.Status = "Completed";
            report.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Analysis completed for work {WorkId}, Plagiarism: {HasPlagiarism}",
                workSubmissionId, report.HasPlagiarism);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analysis for work {WorkId}", workSubmissionId);
            try
            {
                var report = await context.AnalysisReports.FindAsync(reportId);
                if (report != null)
                {
                    report.Status = "Failed";
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Error saving failed status for report {ReportId}", reportId);
            }
        }
    }

    private Dictionary<string, int> AnalyzeText(string text)
    {
        var words = Regex.Matches(text.ToLower(), @"\b[а-яёa-z]{3,}\b")
            .Select(m => m.Value)
            .Where(w => w.Length >= 3)
            .ToList();

        return words
            .GroupBy(w => w)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(kvp => kvp.Value)
            .Take(50)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private async Task<(bool HasPlagiarism, string? Details)> CheckPlagiarismAsync(
        string fileHash, string assignmentId, Guid currentWorkId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var fileStorageUrl = _configuration["FileStorageServiceUrl"] ?? "http://filestoringservice/api";
            
            var response = await httpClient.GetAsync($"{fileStorageUrl}/filestorage/assignment/{assignmentId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var submissions = System.Text.Json.JsonSerializer.Deserialize<List<WorkSubmission>>(
                    content, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (submissions != null)
                {
                    var earlierSubmissions = submissions
                        .Where(s => s.Id != currentWorkId 
                            && s.FileHash == fileHash 
                            && s.SubmittedAt < DateTime.UtcNow)
                        .OrderBy(s => s.SubmittedAt)
                        .ToList();

                    if (earlierSubmissions.Any())
                    {
                        var firstSubmission = earlierSubmissions.First();
                        return (true, 
                            $"Обнаружен плагиат. Работа с таким же содержимым была сдана ранее студентом {firstSubmission.StudentName} " +
                            $"({firstSubmission.SubmittedAt:yyyy-MM-dd HH:mm:ss})");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking plagiarism, continuing without check");
        }

        return (false, null);
    }

    private async Task<string?> GetFileContentAsync(Guid workSubmissionId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var fileStorageUrl = _configuration["FileStorageServiceUrl"] ?? "http://filestoringservice/api";
            
            var response = await httpClient.GetAsync($"{fileStorageUrl}/filestorage/{workSubmissionId}/file");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                if (text.Any(c => char.IsControl(c) && !char.IsWhiteSpace(c) && c != '\r' && c != '\n'))
                {
                    return null;
                }
                return text;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting file content for work {WorkId}, analysis will continue without text", workSubmissionId);
        }

        return null;
    }

    public async Task<AnalysisReport?> GetReportAsync(Guid reportId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        return await context.AnalysisReports.FindAsync(reportId);
    }

    public async Task<List<AnalysisReport>> GetReportsByWorkIdAsync(Guid workId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        return await context.AnalysisReports
            .Where(r => r.WorkSubmissionId == workId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<WorkReportsResponse> GetWorkReportsAsync(Guid workId)
    {
        var reports = await GetReportsByWorkIdAsync(workId);
        
        return new WorkReportsResponse
        {
            WorkId = workId,
            Reports = reports.Select(r => new ReportInfo
            {
                ReportId = r.Id,
                Status = r.Status,
                HasPlagiarism = r.HasPlagiarism,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }
}

