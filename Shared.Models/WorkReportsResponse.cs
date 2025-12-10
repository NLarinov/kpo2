namespace Shared.Models;

public class WorkReportsResponse
{
    public Guid WorkId { get; set; }
    public List<ReportInfo> Reports { get; set; } = new();
}

public class ReportInfo
{
    public Guid ReportId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasPlagiarism { get; set; }
    public DateTime CreatedAt { get; set; }
}

