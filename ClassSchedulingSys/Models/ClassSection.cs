using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSchedulingSys.Models
{
    public class ClassSection
    {
        public int Id { get; set; } // Primary Key

        [Required]
        public string Section { get; set; } = string.Empty;

        public int YearLevel { get; set; }

        public int CollegeCourseId { get; set; }
        [ForeignKey("CollegeCourseId")]
        public CollegeCourse CollegeCourse { get; set; } = null!;

        public int SemesterId { get; set; }
        [ForeignKey("SemesterId")]
        public Semester Semester { get; set; } = null!;

        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

        public ICollection<FacultySubjectAssignment> FacultySubjectAssignments { get; set; } = new List<FacultySubjectAssignment>();

    }
}
