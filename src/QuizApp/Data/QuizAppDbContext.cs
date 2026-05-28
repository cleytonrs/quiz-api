using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizApp.Models;

namespace QuizApp.Data;

public class QuizAppDbContext : IdentityDbContext<ApplicationUser>
{
    public QuizAppDbContext(DbContextOptions<QuizAppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<QuizSession> QuizSessions => Set<QuizSession>();
    public DbSet<SessionAnswer> SessionAnswers => Set<SessionAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Quiz configuration
        builder.Entity<Quiz>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.TopicName).IsRequired().HasMaxLength(200);
            entity.Property(q => q.Description).IsRequired().HasMaxLength(1000);
            entity.Property(q => q.CreatedAt).IsRequired();
            entity.Property(q => q.UpdatedAt).IsRequired();
        });

        // Question configuration
        builder.Entity<Question>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Text).IsRequired().HasMaxLength(1000);
            entity.Property(q => q.OrderIndex).IsRequired();

            entity.HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AnswerOption configuration
        builder.Entity<AnswerOption>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Text).IsRequired().HasMaxLength(500);
            entity.Property(a => a.IsCorrect).IsRequired();

            entity.HasOne(a => a.Question)
                .WithMany(q => q.AnswerOptions)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuizSession configuration
        builder.Entity<QuizSession>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.StartedAt).IsRequired();

            entity.HasOne(s => s.Quiz)
                .WithMany()
                .HasForeignKey(s => s.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Nullable FK to ApplicationUser for guest session support
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Index on UserId for dashboard queries
            entity.HasIndex(s => s.UserId);

            // Index on QuizId for quiz session lookups
            entity.HasIndex(s => s.QuizId);
        });

        // SessionAnswer configuration
        builder.Entity<SessionAnswer>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.QuizSession)
                .WithMany(s => s.Answers)
                .HasForeignKey(a => a.QuizSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(a => a.SelectedAnswerOption)
                .WithMany()
                .HasForeignKey(a => a.SelectedAnswerOptionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Composite index to prevent duplicate answers per question in a session
            entity.HasIndex(a => new { a.QuizSessionId, a.QuestionId }).IsUnique();
        });
    }
}
