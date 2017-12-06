using Microsoft.EntityFrameworkCore;

namespace ProjectManager.Models
{
    public partial class ProjectDBContext : DbContext
    {
        public virtual DbSet<Employees> Employees { get; set; }
        public virtual DbSet<ProjectEmployees> ProjectEmployees { get; set; }
        public virtual DbSet<Projects> Projects { get; set; }
        public virtual DbSet<Transaction> Transaction { get; set; }

        public ProjectDBContext(DbContextOptions<ProjectDBContext> options)
    : base(options)
        { }

        public ProjectDBContext():base()
        {

        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employees>(entity =>
            {
                entity.HasKey(e => e.EmployeeId);

                entity.Property(e => e.BeginningOfWork).HasColumnType("date");

                entity.Property(e => e.Birthday).HasColumnType("date");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(e => e.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<ProjectEmployees>(entity =>
            {
                entity.HasOne(d => d.Employee)
                    .WithMany(p => p.ProjectEmployees)
                    .HasForeignKey(d => d.EmployeeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectEmployees_Employees");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectEmployees)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ProjectEmployees_Projects");
            });

            modelBuilder.Entity<Projects>(entity =>
            {
                entity.HasKey(e => e.ProjectId);

                entity.Property(e => e.Deadline).HasColumnType("date");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Start).HasColumnType("date");
            });
        }
        
        public DbSet<ProjectManager.Models.User> User { get; set; }
    }
}
