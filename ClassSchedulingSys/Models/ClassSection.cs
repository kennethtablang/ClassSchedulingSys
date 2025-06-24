namespace ClassSchedulingSys.Models
{
    public class ClassSection
    {
        public int Id { get; set; } // Primary Key

        // Section label (e.g., A, B, C)
        public string Section { get; set; } = string.Empty;

        // Year level (1st year, 2nd year, etc.)
        public int YearLevel { get; set; }

        // Foreign key to the course this section belongs to
        public int CollegeCourseId { get; set; }
        public CollegeCourse CollegeCourse { get; set; } = null!;

        // Foreign key to the semester this section belongs to
        public int SemesterId { get; set; }
        public Semester Semester { get; set; } = null!;
    }
}
