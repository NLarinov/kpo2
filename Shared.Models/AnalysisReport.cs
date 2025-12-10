namespace Shared.Models;

public class AnalysisReport
{
    public Guid Id { get; set; }
    public Guid WorkSubmissionId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public bool HasPlagiarism { get; set; }
    public string? PlagiarismDetails { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ReportFilePath { get; set; }
    public Dictionary<string, int>? WordFrequency { get; set; } // Для облака слов
}

