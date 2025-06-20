using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation: all users in this department
        public ICollection<ApplicationUser> FacultyMembers { get; set; } = new List<ApplicationUser>();
    }
}
