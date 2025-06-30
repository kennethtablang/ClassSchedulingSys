using System.ComponentModel.DataAnnotations;

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
        public string SubjectType { get; set; }
        public string YearLevel { get; set; }

        public string? Color { get; set; }

        public int CollegeCourseId { get; set; }
        public CollegeCourse CollegeCourse { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Schedule>? Schedules { get; set; }

        public ICollection<FacultySubjectAssignment>? FacultySubjectAssignments { get; set; }

    }
}
