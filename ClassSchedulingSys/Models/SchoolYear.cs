// ClassSchedulingSys/Models/SchoolYear
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class SchoolYear
    {
        public int Id { get; set; }

        [Required]
        public int StartYear { get; set; }

        [Required]
        public int EndYear { get; set; }

        public bool IsCurrent { get; set; } = false;

        public bool IsArchived { get; set; } = false;

        // Optional: Add CreatedAt/UpdatedAt for auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
