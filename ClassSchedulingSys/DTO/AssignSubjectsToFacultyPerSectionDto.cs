using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class AssignSubjectsToFacultyPerSectionDto
    {
        [Required]
        public string FacultyId { get; set; } = string.Empty;

        [Required]
        public List<SubjectSectionAssignment> Assignments { get; set; } = new();
    }

    public class SubjectSectionAssignment
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int ClassSectionId { get; set; }
    }

    public class AssignedSubjectInfoDto
    {
        public int SubjectId { get; set; }
        public int ClassSectionId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
    }

    public class AssignedSubjectSectionDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectTitle { get; set; } = string.Empty;
        public int SubjectUnits { get; set; }
        public string SubjectType { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;

        public int ClassSectionId { get; set; }
        public string SectionLabel { get; set; } = string.Empty;
        public string CollegeCourse { get; set; } = string.Empty;
    }

    public class AssignedSubjectsDto
    {
        public int TotalUnits { get; set; }
        public int TotalSubjects { get; set; }
        public List<AssignedSubjectSectionDto> Subjects { get; set; } = new();
    }

    // This is connected ot the SchedulePage
    public class FacultyAssignmentReadDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectTitle { get; set; } = string.Empty;
        public string SubjectColor { get; set; } = "#999999";

        public string FacultyId { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;

        public int ClassSectionId { get; set; }
        public string SectionLabel { get; set; } = string.Empty;
    }
}
