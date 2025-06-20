using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class SchoolYear
    {
        public int Id { get; set; }

        [Required]
        public string Year { get; set; } = string.Empty; // Format: "2025-2026"

        public ICollection<Semester>? Semesters { get; set; }
    }
}
