namespace ClassSchedulingSys.DTO
{
    // A single unassigned subject row
    public class UnassignedSubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectTitle { get; set; } = string.Empty;
        public int Units { get; set; }
        public string SubjectType { get; set; } = string.Empty;
        public string YearLevel { get; set; } = string.Empty;
        public string? Color { get; set; }
    }

    // Group per class section
    public class UnassignedBySectionDto
    {
        public int ClassSectionId { get; set; }
        public string SectionLabel { get; set; } = string.Empty;
        public int YearLevel { get; set; }
        public int CollegeCourseId { get; set; }
        public string CollegeCourseCode { get; set; } = string.Empty;
        public string CollegeCourseName { get; set; } = string.Empty;

        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public string SchoolYearLabel { get; set; } = string.Empty;

        public int TotalUnassigned { get; set; }
        public List<UnassignedSubjectDto> Subjects { get; set; } = new();
    }

    // Top-level response
    public class UnassignedSubjectsDashboardDto
    {
        public int TotalUnassignedAcrossAllSections { get; set; }
        public List<UnassignedBySectionDto> Sections { get; set; } = new();
    }
}
