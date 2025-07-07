// ClassSchedulingSys/Models/Subject
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClassSchedulingSys.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        public string SubjectCode { get; set; }

        [Required]
        public string SubjectTitle { get; set; }

        public int Units { get; set; }

        [Required]
        public string SubjectType { get; set; }

        [Required]
        public string YearLevel { get; set; }

        public string? Color { get; set; } = "#999999";

        public int CollegeCourseId { get; set; }

        [ForeignKey("CollegeCourseId")]
        public CollegeCourse CollegeCourse { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Schedule>? Schedules { get; set; }

        public ICollection<FacultySubjectAssignment>? FacultySubjectAssignments { get; set; }

    }
}
