using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSchedulingSys.Models
{
    public class ApplicationUser : IdentityUser
    {
        // You can add more properties here if needed, e.g.:
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Computed helper(not mapped to the DB)
        [NotMapped]
        public string FullName
            => string.Join(" ", new[] { FirstName, MiddleName, LastName }
                                .Where(s => !string.IsNullOrWhiteSpace(s)));

        // ← Department linkage
        public int? DepartmentId { get; set; }
        //[ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        //inverse properties
        public ICollection<Schedule>? Schedules { get; set; }

    }
}
