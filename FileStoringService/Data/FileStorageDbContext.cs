using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FileStoringService.Data;

public class FileStorageDbContext : DbContext
{
    public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options) : base(options)
    {
    }

    public DbSet<WorkSubmission> WorkSubmissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StudentName).IsRequired();
            entity.Property(e => e.AssignmentId).IsRequired();
            entity.Property(e => e.FileName).IsRequired();
            entity.Property(e => e.FilePath).IsRequired();
            entity.Property(e => e.FileHash).IsRequired();
            entity.HasIndex(e => e.FileHash);
            entity.HasIndex(e => e.AssignmentId);
        });
    }
}

