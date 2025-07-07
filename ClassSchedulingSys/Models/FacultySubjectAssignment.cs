// ClassSchedulingSys/Models/FacultySubjectAssignment.cs
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.Models
{
    public class FacultySubjectAssignment
    {
        [Required]
        public string FacultyId { get; set; } = string.Empty;

        [ForeignKey("FacultyId")]
        public ApplicationUser Faculty { get; set; } = null!;

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public Subject Subject { get; set; } = null!;

        [Required]
        public int ClassSectionId { get; set; }

        [ForeignKey("ClassSectionId")]
        public ClassSection ClassSection { get; set; } = null!;
    }
}
