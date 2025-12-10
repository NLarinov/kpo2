using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileAnalysisService.Data;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : base(options)
    {
    }

    public DbSet<AnalysisReport> AnalysisReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AnalysisReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.WorkSubmissionId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.HasIndex(e => e.WorkSubmissionId);
            entity.Ignore(e => e.WordFrequency);
        });
    }
}

