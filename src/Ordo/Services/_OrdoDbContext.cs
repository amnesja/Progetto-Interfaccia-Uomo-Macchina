using Ordo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Ordo.Services.Shared;

namespace Ordo.Services
{
    public class OrdoDbContext : DbContext
    {
        public OrdoDbContext()
        {
        }

        public OrdoDbContext(DbContextOptions<OrdoDbContext> options) : base(options)
        {
           // DataGenerator.InitializeUsers(this);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Le relazioni verso User non devono propagare la cancellazione a cascata,
            // altrimenti SQL Server trova percorsi multipli/ambigui verso la stessa tabella.
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Queste invece restano a cascata: cancellare un Project/Board/Task
            // deve eliminare automaticamente ciò che gli appartiene "in proprietà"
            modelBuilder.Entity<Board>()
                .HasOne(b => b.Project)
                .WithMany(p => p.Boards)
                .HasForeignKey(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Board)
                .WithMany(b => b.Tasks)
                .HasForeignKey(t => t.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Task)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}