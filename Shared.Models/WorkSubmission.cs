namespace Shared.Models;

public class WorkSubmission
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
}

