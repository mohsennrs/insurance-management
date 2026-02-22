using Microsoft.EntityFrameworkCore;
using document_management_service.Models;

namespace document_management_service.Data;

public class DocumentsDbContext : DbContext
{
    public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.StorageUrl)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.BucketName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ObjectKey)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Tags)
                .HasMaxLength(500);

            // Indexes for common queries
            entity.HasIndex(e => e.ClaimId);
            entity.HasIndex(e => e.DocumentType);
            entity.HasIndex(e => e.UploadedAt);
        });
    }
}