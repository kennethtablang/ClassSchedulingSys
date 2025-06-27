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

        }
    }

}