using BookIllustration_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookIllustration_Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Character> Characters => Set<Character>();

    public DbSet<Chapter> Chapters => Set<Chapter>();

    public DbSet<PipelineStep> PipelineSteps => Set<PipelineStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(user => user.Projects)
            .WithOne(project => project.User)
            .HasForeignKey(project => project.UserEmail);

        modelBuilder.Entity<Project>()
            .HasMany(project => project.PipelineSteps)
            .WithOne(step => step.Project)
            .HasForeignKey(step => step.ProjectId);

        modelBuilder.Entity<Project>()
            .HasMany(project => project.Characters)
            .WithOne(character => character.Project)
            .HasForeignKey(character => character.ProjectId);

        modelBuilder.Entity<Project>()
            .HasMany(project => project.Chapters)
            .WithOne(chapter => chapter.Project)
            .HasForeignKey(chapter => chapter.ProjectId);

        modelBuilder.Entity<Character>()
            .HasMany(character => character.Chapters)
            .WithMany(chapter => chapter.Characters)
            .UsingEntity("ChapterCharacters");

        modelBuilder.Entity<PipelineStep>()
            .HasIndex(step => new { step.ProjectId, step.StepName })
            .IsUnique();
    }
}
