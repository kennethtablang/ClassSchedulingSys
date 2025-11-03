// ClassSchedulingSys/Data/ApplicationDbContext
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace ClassSchedulingSys.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<SchoolYear> SchoolYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ClassSection> ClassSections { get; set; }
        public DbSet<CollegeCourse> CollegeCourses { get; set; }
        public DbSet<FacultySubjectAssignment> FacultySubjectAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Customizing the ASP.NET Identity model and overriding the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Subject)
                .WithMany(su => su.Schedules)
                .HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // or .NoAction()

            // Configure composite key and relationships for FacultySubjectAssignment
            modelBuilder.Entity<FacultySubjectAssignment>()
                .HasKey(fsa => new { fsa.FacultyId, fsa.SubjectId, fsa.ClassSectionId });

            modelBuilder.Entity<FacultySubjectAssignment>()
                .HasOne(fsa => fsa.Faculty)
                .WithMany(f => f.FacultySubjectAssignments)
                .HasForeignKey(fsa => fsa.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FacultySubjectAssignment>()
                .HasOne(fsa => fsa.Subject)
                .WithMany(s => s.FacultySubjectAssignments)
                .HasForeignKey(fsa => fsa.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FacultySubjectAssignment>()
                .HasOne(fsa => fsa.ClassSection)
                .WithMany(cs => cs.FacultySubjectAssignments)
                .HasForeignKey(fsa => fsa.ClassSectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassSection>()
                .HasOne(cs => cs.SchoolYear)
                .WithMany()
                .HasForeignKey(cs => cs.SchoolYearId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.EmployeeID)
                .IsUnique()
                .HasFilter("[EmployeeID] IS NOT NULL");

            modelBuilder.Entity<Subject>()
                .HasIndex(s => new { s.SubjectCode, s.YearLevel, s.CollegeCourseId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.SubjectCode)
                .IsUnique();

            modelBuilder.Entity<CollegeCourse>()
                .HasIndex(c => c.Code)
                .IsUnique();
        }
    }
}