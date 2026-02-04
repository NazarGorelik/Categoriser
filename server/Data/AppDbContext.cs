using Categoriser.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Categoriser.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobRow> JobRows => Set<JobRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(job => job.Id);
            entity.HasMany(job => job.Rows)
                .WithOne(row => row.Job)
                .HasForeignKey(row => row.JobId);
        });

        modelBuilder.Entity<JobRow>(entity =>
        {
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Isin).HasMaxLength(32);
            entity.Property(row => row.Name).HasMaxLength(256);
            entity.Property(row => row.Wkn).HasMaxLength(32);
            entity.Property(row => row.Category).HasMaxLength(64);
            entity.Property(row => row.SubCategory).HasMaxLength(64);
            entity.Property(row => row.PositionType).HasMaxLength(32);
            entity.Property(row => row.Error).HasMaxLength(512);
        });
    }
}
