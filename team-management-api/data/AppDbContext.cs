using Microsoft.EntityFrameworkCore;
using Shared.Entities;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskActivity> TaskActivities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>()
           .Property(t => t.Status)
           .HasConversion(
    v => v.ToString(),
    v => (Shared.Enums.TaskStatus)Enum.Parse(typeof(Shared.Enums.TaskStatus), v)
      );

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

