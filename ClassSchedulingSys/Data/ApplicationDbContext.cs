using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<Class> Classes { get; set; }

    }
}
