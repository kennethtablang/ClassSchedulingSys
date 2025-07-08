namespace ClassSchedulingSys.DTO
{
    public class ClassSectionDto
    {
        public int Id { get; set; }
        public string Section { get; set; } = string.Empty;
        public int YearLevel { get; set; }

        public int CollegeCourseId { get; set; }
        public string CollegeCourseCode { get; set; } = string.Empty;
        public string CollegeCourseName { get; set; } = string.Empty;

        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;

        public int SchoolYearId { get; set; }
        public string SchoolYearLabel { get; set; } = string.Empty;
    }

    public class CreateClassSectionDto
    {
        public string Section { get; set; } = string.Empty;
        public int YearLevel { get; set; }
        public int CollegeCourseId { get; set; }
        public int SemesterId { get; set; }
        public int SchoolYearId { get; set; } // ✅ newly added
    }

    public class UpdateClassSectionDto
    {
        public int Id { get; set; }
        public string Section { get; set; } = string.Empty;
        public int YearLevel { get; set; }
        public int CollegeCourseId { get; set; }
        public int SemesterId { get; set; }
        public int SchoolYearId { get; set; } // ✅ newly added
    }
}
