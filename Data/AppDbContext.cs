using Microsoft.EntityFrameworkCore;
using UserManagementApp.Models;

namespace UserManagementApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Store UserStatus enum as string in the database
        modelBuilder.Entity<User>()
            .Property(u => u.Status)
            .HasConversion<string>();

        // Requirement: Database-level UNIQUE INDEX on Email to ensure consistency at the storage level
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email_unique");
    }
}