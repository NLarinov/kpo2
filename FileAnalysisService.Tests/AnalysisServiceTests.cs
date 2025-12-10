using FileAnalysisService.Data;
using FileAnalysisService.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace FileAnalysisService.Tests;

public class AnalysisServiceTests
{
    private readonly AnalysisDbContext _context;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<AnalysisService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AnalysisService _service;

    public AnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AnalysisDbContext(options);

        _environmentMock = new Mock<IWebHostEnvironment>();
        _environmentMock.Setup(e => e.ContentRootPath).Returns("/test");

        _loggerMock = new Mock<ILogger<AnalysisService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();

        _service = new AnalysisService(
            _context,
            _environmentMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task StartAnalysisAsync_ShouldCreateReport()
    {
        var workSubmissionId = Guid.NewGuid();
        var fileHash = "hash123";
        var assignmentId = "ASSIGNMENT-001";

        var result = await _service.StartAnalysisAsync(workSubmissionId, fileHash, assignmentId);

        Assert.NotNull(result);
        Assert.Equal(workSubmissionId, result.WorkSubmissionId);
        Assert.Equal("Pending", result.Status);
        Assert.False(result.HasPlagiarism);
    }

    [Fact]
    public async Task GetReportAsync_ShouldReturnReport()
    {
        var report = new AnalysisReport
        {
            Id = Guid.NewGuid(),
            WorkSubmissionId = Guid.NewGuid(),
            Status = "Completed",
            HasPlagiarism = false,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        _context.AnalysisReports.Add(report);
        await _context.SaveChangesAsync();

        var result = await _service.GetReportAsync(report.Id);

        Assert.NotNull(result);
        Assert.Equal(report.Id, result.Id);
        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task GetReportAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _service.GetReportAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorkReportsAsync_ShouldReturnReportsForWork()
    {
        var workId = Guid.NewGuid();
        var report1 = new AnalysisReport
        {
            Id = Guid.NewGuid(),
            WorkSubmissionId = workId,
            Status = "Completed",
            HasPlagiarism = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var report2 = new AnalysisReport
        {
            Id = Guid.NewGuid(),
            WorkSubmissionId = workId,
            Status = "Completed",
            HasPlagiarism = true,
            CreatedAt = DateTime.UtcNow
        };
        var report3 = new AnalysisReport
        {
            Id = Guid.NewGuid(),
            WorkSubmissionId = Guid.NewGuid(),
            Status = "Completed",
            HasPlagiarism = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.AnalysisReports.AddRange(report1, report2, report3);
        await _context.SaveChangesAsync();

        var result = await _service.GetWorkReportsAsync(workId);

        Assert.NotNull(result);
        Assert.Equal(workId, result.WorkId);
        Assert.Equal(2, result.Reports.Count);
        Assert.Contains(result.Reports, r => r.ReportId == report1.Id);
        Assert.Contains(result.Reports, r => r.ReportId == report2.Id);
    }
}

