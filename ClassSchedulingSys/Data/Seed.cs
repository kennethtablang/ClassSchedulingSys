using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Data
{
    public class Seed
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            // Acquire required services
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Ensure DB is created and up-to-date
            await context.Database.MigrateAsync();

            // Seed Roles
            string[] roles = new[] { "SuperAdmin", "Dean", "Faculty" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Seed Departments
            if (!context.Departments.Any())
            {
                context.Departments.AddRange(
                    new Department { Name = "Computer Science", Description = "CS Dept." },
                    new Department { Name = "Business Administration", Description = "BA Dept." },
                    new Department { Name = "Education", Description = "Education Dept." }
                );
                await context.SaveChangesAsync();
            }

            // Optional: Seed a default SuperAdmin user
            var adminEmail = "admin@pcnl.edu";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    DepartmentId = null
                };
                var result = await userManager.CreateAsync(adminUser, "Admin#123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                }
            }
        }
    }
}
