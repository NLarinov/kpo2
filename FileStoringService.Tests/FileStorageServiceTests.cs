using FileStoringService.Data;
using FileStoringService.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace FileStoringService.Tests;

public class FileStorageServiceTests
{
    private readonly FileStorageDbContext _context;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly Mock<ILogger<FileStorageService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly FileStorageService _service;

    public FileStorageServiceTests()
    {
        var options = new DbContextOptionsBuilder<FileStorageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new FileStorageDbContext(options);

        _environmentMock = new Mock<IWebHostEnvironment>();
        _environmentMock.Setup(e => e.ContentRootPath).Returns("/test");

        _loggerMock = new Mock<ILogger<FileStorageService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configurationMock = new Mock<IConfiguration>();

        _service = new FileStorageService(
            _context,
            _environmentMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task StoreFileAsync_ShouldCreateSubmission()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("test.txt");
        fileMock.Setup(f => f.Length).Returns(100L);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        _environmentMock.Setup(e => e.ContentRootPath).Returns(tempPath);

        var request = new WorkSubmissionRequest
        {
            StudentName = "Test Student",
            AssignmentId = "ASSIGNMENT-001"
        };

        var result = await _service.StoreFileAsync(fileMock.Object, request);

        Assert.NotNull(result);
        Assert.Equal("Test Student", result.StudentName);
        Assert.Equal("ASSIGNMENT-001", result.AssignmentId);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task GetSubmissionAsync_ShouldReturnSubmission()
    {
        var submission = new WorkSubmission
        {
            Id = Guid.NewGuid(),
            StudentName = "Test Student",
            AssignmentId = "ASSIGNMENT-001",
            SubmittedAt = DateTime.UtcNow,
            FileName = "test.txt",
            FilePath = "/test/test.txt",
            FileHash = "hash123"
        };
        _context.WorkSubmissions.Add(submission);
        await _context.SaveChangesAsync();

        var result = await _service.GetSubmissionAsync(submission.Id);

        Assert.NotNull(result);
        Assert.Equal(submission.Id, result.Id);
        Assert.Equal("Test Student", result.StudentName);
    }

    [Fact]
    public async Task GetSubmissionAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _service.GetSubmissionAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSubmissionsByAssignmentAsync_ShouldReturnFilteredSubmissions()
    {
        var assignmentId = "ASSIGNMENT-001";
        var submission1 = new WorkSubmission
        {
            Id = Guid.NewGuid(),
            StudentName = "Student 1",
            AssignmentId = assignmentId,
            SubmittedAt = DateTime.UtcNow,
            FileName = "test1.txt",
            FilePath = "/test/test1.txt",
            FileHash = "hash1"
        };
        var submission2 = new WorkSubmission
        {
            Id = Guid.NewGuid(),
            StudentName = "Student 2",
            AssignmentId = assignmentId,
            SubmittedAt = DateTime.UtcNow,
            FileName = "test2.txt",
            FilePath = "/test/test2.txt",
            FileHash = "hash2"
        };
        var submission3 = new WorkSubmission
        {
            Id = Guid.NewGuid(),
            StudentName = "Student 3",
            AssignmentId = "ASSIGNMENT-002",
            SubmittedAt = DateTime.UtcNow,
            FileName = "test3.txt",
            FilePath = "/test/test3.txt",
            FileHash = "hash3"
        };

        _context.WorkSubmissions.AddRange(submission1, submission2, submission3);
        await _context.SaveChangesAsync();

        var result = await _service.GetSubmissionsByAssignmentAsync(assignmentId);

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(assignmentId, s.AssignmentId));
    }
}
