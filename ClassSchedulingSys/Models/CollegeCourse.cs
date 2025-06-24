using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class CollegeCourse
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = null!; // Example: BSIT, BSED

        [Required]
        public string Name { get; set; } = null!; // Example: Bachelor of Science in Information Technology

        public bool IsArchived { get; set; } = false;

        // Relationships
        public ICollection<ClassSection>? ClassSections { get; set; } // One-to-many (a course can have multiple sections)
    }
}
