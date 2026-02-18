
using Microsoft.EntityFrameworkCore;
using insurance_claim.Models;

namespace insurance_claim.Data;

public class ClaimsDbContext : DbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : base(options)
    {
    }

    public DbSet<Claim> Claims { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Claim>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ClaimNumber)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.HasIndex(e => e.ClaimNumber)
                .IsUnique();
            
            entity.Property(e => e.PolicyNumber)
                .IsRequired()
                .HasMaxLength(50);
            
            entity.Property(e => e.ClaimAmount)
                .HasColumnType("decimal(18,2)");
            
            entity.Property(e => e.ClaimantName)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.ClaimantEmail)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.ClaimantPhone)
                .HasMaxLength(20);
            
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(2000);
            
            entity.Property(e => e.AssignedTo)
                .HasMaxLength(200);
            
            entity.Property(e => e.Notes)
                .HasMaxLength(2000);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Indexes for common queries
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PolicyNumber);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}